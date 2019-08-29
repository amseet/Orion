from tensorflow.python.framework import ops
from tensorflow.python.keras.utils import losses_utils
from tensorflow.python.ops import control_flow_ops
from tensorflow.python.ops import nn
from tensorflow.python.ops import partitioned_variables
from tensorflow.python.ops import state_ops
from tensorflow.python.ops import variable_scope
from tensorflow.python.ops.losses import losses
from tensorflow.python.summary import summary
from tensorflow.python.training import sync_replicas_optimizer
from tensorflow.python.training import training_util
from tensorflow.python.util.tf_export import estimator_export
from tensorflow_estimator.python.estimator import estimator
from tensorflow_estimator.python.estimator.canned import dnn
from tensorflow_estimator.python.estimator.canned import head as head_lib
from tensorflow_estimator.python.estimator.canned import linear
from tensorflow_estimator.python.estimator.canned import optimizers
from tensorflow_estimator.python.estimator.head import head_utils
from tensorflow_estimator.python.estimator.head import regression_head


# The default learning rates are a historical artifact of the initial
# implementation.
_DNN_LEARNING_RATE = 0.001
_LINEAR_LEARNING_RATE = 0.005

def _check_no_sync_replicas_optimizer(optimizer):
  if isinstance(optimizer, sync_replicas_optimizer.SyncReplicasOptimizer):
    raise ValueError(
        'SyncReplicasOptimizer does not support multi optimizers case. '
        'Therefore, it is not supported in DNNLinearCombined model. '
        'If you want to use this optimizer, please use either DNN or Linear '
        'model.')

def _linear_learning_rate(num_linear_feature_columns):
  """Returns the default learning rate of the linear model.
  The calculation is a historical artifact of this initial implementation, but
  has proven a reasonable choice.
  Args:
    num_linear_feature_columns: The number of feature columns of the linear
      model.
  Returns:
    A float.
  """
  default_learning_rate = 1. / math.sqrt(num_linear_feature_columns)
  return min(_LINEAR_LEARNING_RATE, default_learning_rate)


def _add_layer_summary(value, tag):
  summary.scalar('%s/fraction_of_zero_values' % tag, nn.zero_fraction(value))
  summary.histogram('%s/activation' % tag, value)


def _validate_feature_columns(linear_feature_columns, dnn_feature_columns):
  """Validates feature columns DNNLinearCombinedRegressor."""
  linear_feature_columns = linear_feature_columns or []
  dnn_feature_columns = dnn_feature_columns or []
  feature_columns = (
      list(linear_feature_columns) + list(dnn_feature_columns))
  if not feature_columns:
    raise ValueError('Either linear_feature_columns or dnn_feature_columns '
                     'must be defined.')
  return feature_columns

def dnn_linear_combined_model(
    features,
    mode,
    linear_feature_columns=None,
    linear_optimizer='Ftrl',
    dnn_feature_columns=None,
    dnn_optimizer='Adagrad',
    dnn_hidden_units=None,
    dnn_activation_fn=nn.relu,
    dnn_dropout=None,
    config=None,
    batch_norm=False,
    linear_sparse_combiner='sum',
    loss_reduction= losses.ReductionV2.SUM_OVER_BATCH_SIZE):
  """Deep Neural Net and Linear combined model_fn.
  Args:
    features: dict of `Tensor`.
    mode: Defines whether this is training, evaluation or prediction. See
      `ModeKeys`.
    linear_feature_columns: An iterable containing all the feature columns used
      by the Linear model.
    linear_optimizer: string, `Optimizer` object, or callable that defines the
      optimizer to use for training the Linear model. Defaults to the Ftrl
      optimizer.
    dnn_feature_columns: An iterable containing all the feature columns used by
      the DNN model.
    dnn_optimizer: string, `Optimizer` object, or callable that defines the
      optimizer to use for training the DNN model. Defaults to the Adagrad
      optimizer.
    dnn_hidden_units: List of hidden units per DNN layer.
    dnn_activation_fn: Activation function applied to each DNN layer. If `None`,
      will use `tf.nn.relu`.
    dnn_dropout: When not `None`, the probability we will drop out a given DNN
      coordinate.
    config: `RunConfig` object to configure the runtime settings.
    batch_norm: Whether to use batch normalization after each hidden layer.
    linear_sparse_combiner: A string specifying how to reduce the linear model
      if a categorical column is multivalent.  One of "mean", "sqrtn", and
      "sum".
    loss_reduction: One of `tf.keras.losses.Reduction` except `NONE`. Describes
      how to reduce training loss over batch. Defaults to `SUM_OVER_BATCH_SIZE`.
  Returns:
    An `EstimatorSpec` instance.
  Raises:
    ValueError: If both `linear_feature_columns` and `dnn_features_columns`
      are empty at the same time, or `input_layer_partitioner` is missing,
      or features has the wrong type.
  """
  if not isinstance(features, dict):
    raise ValueError('features should be a dictionary of `Tensor`s. '
                     'Given type: {}'.format(type(features)))
  if not linear_feature_columns and not dnn_feature_columns:
    raise ValueError(
        'Either linear_feature_columns or dnn_feature_columns must be defined.')

  del config

  head = regression_head.RegressionHead(
        label_dimension=1,
        weight_column=None,
        loss_reduction=loss_reduction)

  # Build DNN Logits.
  if not dnn_feature_columns:
    dnn_logits = None
  else:
    if mode == ModeKeys.TRAIN:
      dnn_optimizer = optimizers.get_optimizer_instance_v2(
          dnn_optimizer, learning_rate=_DNN_LEARNING_RATE)
      _check_no_sync_replicas_optimizer(dnn_optimizer)

    if not dnn_hidden_units:
      raise ValueError(
          'dnn_hidden_units must be defined when dnn_feature_columns is '
          'specified.')
    dnn_logits, dnn_trainable_variables, dnn_update_ops = (
        dnn._dnn_model_fn_builder_v2(  # pylint: disable=protected-access
            units=head.logits_dimension,
            hidden_units=dnn_hidden_units,
            feature_columns=dnn_feature_columns,
            activation_fn=dnn_activation_fn,
            dropout=dnn_dropout,
            batch_norm=batch_norm,
            features=features,
            mode=mode))

  if not linear_feature_columns:
    linear_logits = None
  else:
    if mode == ModeKeys.TRAIN:
      linear_optimizer = optimizers.get_optimizer_instance_v2(
          linear_optimizer,
          learning_rate=_linear_learning_rate(len(linear_feature_columns)))
      _check_no_sync_replicas_optimizer(linear_optimizer)

    linear_logits, linear_trainable_variables = (
        linear._linear_model_fn_builder_v2(  # pylint: disable=protected-access
            units=head.logits_dimension,
            feature_columns=linear_feature_columns,
            sparse_combiner=linear_sparse_combiner,
            features=features))
    _add_layer_summary(linear_logits, 'linear')

  # Combine logits and build full model.
  if dnn_logits is not None and linear_logits is not None:
    logits = dnn_logits + linear_logits
  elif dnn_logits is not None:
    logits = dnn_logits
  else:
    logits = linear_logits

  def _train_op_fn(loss):
    """Returns the op to optimize the loss."""
    train_ops = []
    # Scale loss by number of replicas.
    if loss_reduction == losses_utils.ReductionV2.SUM_OVER_BATCH_SIZE:
      loss = losses_utils.scale_loss_for_distribution(loss)

    if dnn_logits is not None:
      train_ops.extend(
          dnn_optimizer.get_updates(
              loss,
              dnn_trainable_variables))
      if dnn_update_ops is not None:
        train_ops.extend(dnn_update_ops)
    if linear_logits is not None:
      train_ops.extend(
          linear_optimizer.get_updates(
              loss,
              linear_trainable_variables))
    train_op = control_flow_ops.group(*train_ops)
    return train_op

  # In TRAIN mode, asssign global_step variable to optimizer.iterations to
  # make global_step increased correctly, as Hooks relies on global step as
  # step counter. Note that, Only one model's optimizer needs this assignment.
  if mode == ModeKeys.TRAIN:
    if dnn_logits is not None:
      dnn_optimizer.iterations = training_util.get_or_create_global_step()
    else:
      linear_optimizer.iterations = training_util.get_or_create_global_step()

  return logits