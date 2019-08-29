from graph_nets import graphs, blocks, utils_tf
import sonnet as snt
import tensorflow as tf

from tensorflow.python.keras.models import Sequential
from tensorflow.python.keras.layers import Input, Dense, Dropout, GRU
from tensorflow.python.keras.optimizers import SGD
from tensorflow.python.feature_column import feature_column
from tensorflow.python.feature_column import feature_column_lib
from tensorflow.python.framework import ops
from tensorflow.python.keras.engine import training
from tensorflow.python.keras.layers import core as keras_core
from tensorflow.python.keras.layers import normalization as keras_norm
from tensorflow.python.keras.utils import losses_utils
from tensorflow.python.layers import core as core_layers
from tensorflow.python.layers import normalization
from tensorflow.python.ops import init_ops
from tensorflow.python.ops import nn
from tensorflow.python.ops import partitioned_variables
from tensorflow.python.ops import variable_scope
from tensorflow.python.ops.losses import losses
from tensorflow.python.summary import summary
from tensorflow.python.training import training_util
from tensorflow.python.util.tf_export import estimator_export
from tensorflow_estimator.python.estimator import estimator
from tensorflow_estimator.python.estimator.canned import head as head_lib
from tensorflow_estimator.python.estimator.canned import optimizers
from tensorflow_estimator.python.estimator.head import head_utils
from tensorflow_estimator.python.estimator.head import regression_head


class GNN(snt.AbstractModule):
  """Implementation of a Graph Network."""

  def __init__(self,
               edge_model_fn,
               node_model_fn,
               reducer=tf.unsorted_segment_sum,
               name="graph_network"):
    """Initializes the GraphNetwork module.
    Args:
      edge_model_fn: A callable that will be passed to EdgeBlock to perform
        per-edge computations. The callable must return a Sonnet module (or
        equivalent; see EdgeBlock for details).

      node_model_fn: A callable that will be passed to NodeBlock to perform
        per-node computations. The callable must return a Sonnet module (or
        equivalent; see NodeBlock for details).
     
      reducer: Reducer to be used by NodeBlock and GlobalBlock to aggregate
        nodes and edges. Defaults to tf.unsorted_segment_sum. This will be
        overridden by the reducers specified in `node_block_opt` and
        `global_block_opt`, if any.

      name: The module name.
    """
    super(GNN, self).__init__(name=name)
    
    with self._enter_variable_scope():
        self._edge_block = blocks.EdgeBlock(
            edge_model_fn=edge_model_fn,
            use_edges=True,
            use_receiver_nodes=True,
            use_sender_nodes=True,
            use_globals=True)
        self._node_block = blocks.NodeBlock(
            node_model_fn=node_model_fn,
            use_received_edges=True,
            use_sent_edges=False,
            use_nodes=True,
            use_globals=True,
            received_edges_reducer=reducer)

  def _build(self, graph):
    """Connects the GraphNetwork.
    Args:
      graph: A `graphs.GraphsTuple` containing `Tensor`s. Depending on the block
        options, `graph` may contain `None` fields; but with the default
        configuration, no `None` field is allowed. Moreover, when using the
        default configuration, the features of each nodes, edges and globals of
        `graph` should be concatenable on the last dimension.
    Returns:
      An output `graphs.GraphsTuple` with updated edges, and nodes.
    """

    return self._node_block(self._edge_block(graph))

def RegressorGRU(hidden_size, input_shape, dropout_rate):
    # The GRU architecture
    regressorGRU = Sequential()
    # First GRU layer with Dropout regularisation
    regressorGRU.add(GRU(units=hidden_size, return_sequences=True, input_shape=input_shape, activation='tanh'))
    regressorGRU.add(Dropout(rate=dropout_rate))
    # Second GRU layer
    regressorGRU.add(GRU(units=hidden_size, return_sequences=True, activation='tanh'))
    regressorGRU.add(Dropout(rate=dropout_rate))
    # Third GRU layer
    regressorGRU.add(GRU(units=hidden_size, return_sequences=True, activation='tanh'))
    regressorGRU.add(Dropout(rate=dropout_rate))
    # Fourth GRU layer
    regressorGRU.add(GRU(units=hidden_size, return_sequences=True, activation='tanh'))
    regressorGRU.add(Dropout(rate=dropout_rate))
    # The output layer
    regressorGRU.add(Dense(units=1))
    # Compiling the RNN
    regressorGRU.compile(optimizer=SGD(lr=0.01, decay=1e-7, momentum=0.9, nesterov=False), loss='mean_squared_error')

    return snt.Module(regressorGRU, name='RegressorGRU')

class RNN(snt.AbstractModule):
   
    def __init__(self, hidden_size, output_size, batch_size, nonlinearity=tf.tanh, name="GRU"):
        super(RNN, self).__init__(name=name)
        self._hidden_size = hidden_size
        self._output_size = output_size
        self._batch_size = batch_size

        

    def _build(self, input):

        lstm2, _ = snt.lstm_with_recurrent_dropout(self._hidden_size, 0.8)
        lstm3, _ = snt.lstm_with_recurrent_dropout(self._hidden_size, 0.5)
        lstm4, _ = snt.lstm_with_recurrent_dropout(self._hidden_size, 0.5)
        dense = Dense(self._output_size)
        #self.model = snt.Sequential([lstm1, lstm2, lstm3, lstm4, dense])

        #initial_state  = self.model.initial_state(self._batch_size)
        #output_sequence, final_state = tf.nn.dynamic_rnn(self.model, input, time_major=True)
        return snt.Sequential([lstm1, lstm2, lstm3, lstm4, dense])

class DNNModel(snt.AbstractModule):
   
    def __init__(self, units, hidden_units, feature_columns, activation_fn, dropout, batch_norm, name='DNNModel', **kwargs):
        super(_DNNModelV2, self).__init__(name=name, **kwargs)

        with ops.name_scope(
            'input_from_feature_columns') as input_feature_column_scope:
            layer_name = input_feature_column_scope + 'input_layer'
            if feature_column_lib.is_feature_column_v2(feature_columns):
                self._input_layer = dense_features_v2.DenseFeatures(
                    feature_columns=feature_columns, name=layer_name)
            else:
                raise ValueError(
                    'Received a feature column from TensorFlow v1, but this is a '
                    'TensorFlow v2 Estimator. Please either use v2 feature columns '
                    '(accessible via tf.feature_column.* in TF 2.x) with this '
                    'Estimator, or switch to a v1 Estimator for use with v1 feature '
                    'columns (accessible via tf.compat.v1.estimator.* and '
                    'tf.compat.v1.feature_column.*, respectively.')

        self._dropout = dropout
        self._batch_norm = batch_norm

        self._hidden_layers = []
        self._dropout_layers = []
        self._batch_norm_layers = []
        self._hidden_layer_scope_names = []
        for layer_id, num_hidden_units in enumerate(hidden_units):
            with ops.name_scope('hiddenlayer_%d' % layer_id) as hidden_layer_scope:
                # Get scope name without the trailing slash.
                hidden_shared_name = _name_from_scope_name(hidden_layer_scope)
                hidden_layer = keras_core.Dense(
                    units=num_hidden_units,
                    activation=activation_fn,
                    kernel_initializer=init_ops.glorot_uniform_initializer(),
                    name=hidden_shared_name)
                self._hidden_layer_scope_names.append(hidden_shared_name)
                self._hidden_layers.append(hidden_layer)
                if self._dropout is not None:
                    dropout_layer = keras_core.Dropout(rate=self._dropout)
                    self._dropout_layers.append(dropout_layer)
                if self._batch_norm:
                    batch_norm_name = hidden_shared_name + '/batchnorm_%d' % layer_id
                    batch_norm_layer = keras_norm.BatchNormalization(
                        # The default momentum 0.99 actually crashes on certain
                        # problem, so here we use 0.999, which is the default of
                        # tf.contrib.layers.batch_norm.
                        momentum=0.999,
                        trainable=True,
                        name=batch_norm_name)
                    self._batch_norm_layers.append(batch_norm_layer)

        with ops.name_scope('logits') as logits_scope:
            logits_shared_name = _name_from_scope_name(logits_scope)
            self._logits_layer = keras_core.Dense(
                units=units,
                activation=None,
                kernel_initializer=init_ops.glorot_uniform_initializer(),
                name=logits_shared_name)
            self._logits_scope_name = logits_shared_name


    def _build(self, features):
        is_training = mode == ModeKeys.TRAIN
        net = self._input_layer(features)
        for i in range(len(self._hidden_layers)):
          net = self._hidden_layers[i](net)
          if self._dropout is not None and is_training:
            net = self._dropout_layers[i](net, training=True)
          if self._batch_norm:
            net = self._batch_norm_layers[i](net, training=is_training)
          _add_hidden_layer_summary(net, self._hidden_layer_scope_names[i])

        logits = self._logits_layer(net)
        _add_hidden_layer_summary(logits, self._logits_scope_name)
        return logits

class NoModel(snt.AbstractModule):
    def __init__(self, name="NoModel"):
        super(LabelModel, self).__init__(name=name)
        
    def _build(self, input):
        return input


NODES = graphs.NODES
EDGES = graphs.EDGES
GLOBALS = graphs.GLOBALS
RECEIVERS = graphs.RECEIVERS
SENDERS = graphs.SENDERS
GLOBALS = graphs.GLOBALS
N_NODE = graphs.N_NODE
N_EDGE = graphs.N_EDGE

class RecurrentEdgeBlock(snt.AbstractModule):
  """Edge block.
  A block that updates the features of each edge in a batch of graphs based on
  (a subset of) the previous edge features, the features of the adjacent nodes,
  and the global features of the corresponding graph.
  See https://arxiv.org/abs/1806.01261 for more details.
  """

  def __init__(self,
               edge_model_fn,
               use_edges=True,
               use_receiver_nodes=True,
               use_sender_nodes=True,
               use_globals=True,
               name="recurrent_edge_block"):
    """Initializes the EdgeBlock module.
    Args:
      edge_model_fn: A callable that will be called in the variable scope of
        this EdgeBlock and should return a Sonnet module (or equivalent
        callable) to be used as the edge model. The returned module should take
        a `Tensor` (of concatenated input features for each edge) and return a
        `Tensor` (of output features for each edge). Typically, this module
        would input and output `Tensor`s of rank 2, but it may also be input or
        output larger ranks. See the `_build` method documentation for more
        details on the acceptable inputs to this module in that case.
      use_edges: (bool, default=True). Whether to condition on edge attributes.
      use_receiver_nodes: (bool, default=True). Whether to condition on receiver
        node attributes.
      use_sender_nodes: (bool, default=True). Whether to condition on sender
        node attributes.
      use_globals: (bool, default=True). Whether to condition on global
        attributes.
      name: The module name.
    Raises:
      ValueError: When fields that are required are missing.
    """
    super(RecurrentEdgeBlock, self).__init__(name=name)

    if not (use_edges or use_sender_nodes or use_receiver_nodes or use_globals):
      raise ValueError("At least one of use_edges, use_sender_nodes, "
                       "use_receiver_nodes or use_globals must be True.")

    self._use_edges = use_edges
    self._use_receiver_nodes = use_receiver_nodes
    self._use_sender_nodes = use_sender_nodes
    self._use_globals = use_globals

    with self._enter_variable_scope():
      self._edge_model = edge_model_fn()

  def _build(self, graph, prev_state):
    """Connects the edge block.
    Args:
      graph: A `graphs.GraphsTuple` containing `Tensor`s, whose individual edges
        features (if `use_edges` is `True`), individual nodes features (if
        `use_receiver_nodes` or `use_sender_nodes` is `True`) and per graph
        globals (if `use_globals` is `True`) should be concatenable on the last
        axis.
    Returns:
      An output `graphs.GraphsTuple` with updated edges.
    Raises:
      ValueError: If `graph` does not have non-`None` receivers and senders, or
        if `graph` has `None` fields incompatible with the selected `use_edges`,
        `use_receiver_nodes`, `use_sender_nodes`, or `use_globals` options.
    """
    blocks._validate_graph(
        graph, (SENDERS, RECEIVERS, N_EDGE), " when using an EdgeBlock")

    edges_to_collect = []

    if self._use_edges:
      blocks._validate_graph(graph, (EDGES,), "when use_edges == True")
      edges_to_collect.append(graph.edges)

    if self._use_receiver_nodes:
      edges_to_collect.append(blocks.broadcast_receiver_nodes_to_edges(graph))

    if self._use_sender_nodes:
      edges_to_collect.append(blocks.broadcast_sender_nodes_to_edges(graph))

    if self._use_globals:
      edges_to_collect.append(blocks.broadcast_globals_to_edges(graph))

    collected_edges = tf.concat(edges_to_collect, axis=-1)
    updated_edges, updated_state = self._edge_model(collected_edges, prev_state)
    return graph.replace(edges=updated_edges), updated_state

class RecurrentNodeBlock(snt.AbstractModule):
  """Node block.
  A block that updates the features of each node in batch of graphs based on
  (a subset of) the previous node features, the aggregated features of the
  adjacent edges, and the global features of the corresponding graph.
  See https://arxiv.org/abs/1806.01261 for more details.
  """

  def __init__(self,
               node_model_fn,
               use_received_edges=True,
               use_sent_edges=False,
               use_nodes=True,
               use_globals=True,
               received_edges_reducer=tf.unsorted_segment_sum,
               sent_edges_reducer=tf.unsorted_segment_sum,
               name="recurrent_node_block"):
    """Initializes the NodeBlock module.
    Args:
      node_model_fn: A callable that will be called in the variable scope of
        this NodeBlock and should return a Sonnet module (or equivalent
        callable) to be used as the node model. The returned module should take
        a `Tensor` (of concatenated input features for each node) and return a
        `Tensor` (of output features for each node). Typically, this module
        would input and output `Tensor`s of rank 2, but it may also be input or
        output larger ranks. See the `_build` method documentation for more
        details on the acceptable inputs to this module in that case.
      use_received_edges: (bool, default=True) Whether to condition on
        aggregated edges received by each node.
      use_sent_edges: (bool, default=False) Whether to condition on aggregated
        edges sent by each node.
      use_nodes: (bool, default=True) Whether to condition on node attributes.
      use_globals: (bool, default=True) Whether to condition on global
        attributes.
      received_edges_reducer: Reduction to be used when aggregating received
        edges. This should be a callable whose signature matches
        `tf.unsorted_segment_sum`.
      sent_edges_reducer: Reduction to be used when aggregating sent edges.
        This should be a callable whose signature matches
        `tf.unsorted_segment_sum`.
      name: The module name.
    Raises:
      ValueError: When fields that are required are missing.
    """

    super(RecurrentNodeBlock, self).__init__(name=name)

    if not (use_nodes or use_sent_edges or use_received_edges or use_globals):
      raise ValueError("At least one of use_received_edges, use_sent_edges, "
                       "use_nodes or use_globals must be True.")

    self._use_received_edges = use_received_edges
    self._use_sent_edges = use_sent_edges
    self._use_nodes = use_nodes
    self._use_globals = use_globals

    with self._enter_variable_scope():
      self._node_model = node_model_fn()
      if self._use_received_edges:
        if received_edges_reducer is None:
          raise ValueError(
              "If `use_received_edges==True`, `received_edges_reducer` "
              "should not be None.")
        self._received_edges_aggregator = blocks.ReceivedEdgesToNodesAggregator(
            received_edges_reducer)
      if self._use_sent_edges:
        if sent_edges_reducer is None:
          raise ValueError(
              "If `use_sent_edges==True`, `sent_edges_reducer` "
              "should not be None.")
        self._sent_edges_aggregator = blocks.SentEdgesToNodesAggregator(
            sent_edges_reducer)

  def _build(self, graph, prev_state):
    """Connects the node block.
    Args:
      graph: A `graphs.GraphsTuple` containing `Tensor`s, whose individual edges
        features (if `use_received_edges` or `use_sent_edges` is `True`),
        individual nodes features (if `use_nodes` is True) and per graph globals
        (if `use_globals` is `True`) should be concatenable on the last axis.
    Returns:
      An output `graphs.GraphsTuple` with updated nodes.
    """

    nodes_to_collect = []

    if self._use_received_edges:
      nodes_to_collect.append(self._received_edges_aggregator(graph))

    if self._use_sent_edges:
      nodes_to_collect.append(self._sent_edges_aggregator(graph))

    if self._use_nodes:
      blocks._validate_graph(graph, (NODES,), "when use_nodes == True")
      nodes_to_collect.append(graph.nodes)

    if self._use_globals:
      nodes_to_collect.append(blocks.broadcast_globals_to_nodes(graph))

    collected_nodes = tf.concat(nodes_to_collect, axis=-1)
    updated_nodes, update_state = self._node_model(collected_nodes, prev_state)
    return graph.replace(nodes=updated_nodes), update_state

class RecurrentGlobalBlock(snt.AbstractModule):
  """Global block.
  A block that updates the global features of each graph in a batch based on
  (a subset of) the previous global features, the aggregated features of the
  edges of the graph, and the aggregated features of the nodes of the graph.
  See https://arxiv.org/abs/1806.01261 for more details.
  """

  def __init__(self,
               global_model_fn,
               use_edges=True,
               use_nodes=True,
               use_globals=True,
               nodes_reducer=tf.unsorted_segment_sum,
               edges_reducer=tf.unsorted_segment_sum,
               name="recurrent_global_block"):
    """Initializes the GlobalBlock module.
    Args:
      global_model_fn: A callable that will be called in the variable scope of
        this GlobalBlock and should return a Sonnet module (or equivalent
        callable) to be used as the global model. The returned module should
        take a `Tensor` (of concatenated input features) and return a `Tensor`
        (the global output features). Typically, this module would input and
        output `Tensor`s of rank 2, but it may also input or output larger
        ranks. See the `_build` method documentation for more details on the
        acceptable inputs to this module in that case.
      use_edges: (bool, default=True) Whether to condition on aggregated edges.
      use_nodes: (bool, default=True) Whether to condition on node attributes.
      use_globals: (bool, default=True) Whether to condition on global
        attributes.
      nodes_reducer: Reduction to be used when aggregating nodes. This should
        be a callable whose signature matches tf.unsorted_segment_sum.
      edges_reducer: Reduction to be used when aggregating edges. This should
        be a callable whose signature matches tf.unsorted_segment_sum.
      name: The module name.
    Raises:
      ValueError: When fields that are required are missing.
    """

    super(RecurrentGlobalBlock, self).__init__(name=name)

    if not (use_nodes or use_edges or use_globals):
      raise ValueError("At least one of use_edges, "
                       "use_nodes or use_globals must be True.")

    self._use_edges = use_edges
    self._use_nodes = use_nodes
    self._use_globals = use_globals

    with self._enter_variable_scope():
      self._global_model = global_model_fn()
      if self._use_edges:
        if edges_reducer is None:
          raise ValueError(
              "If `use_edges==True`, `edges_reducer` should not be None.")
        self._edges_aggregator = blocks.EdgesToGlobalsAggregator(
            edges_reducer)
      if self._use_nodes:
        if nodes_reducer is None:
          raise ValueError(
              "If `use_nodes==True`, `nodes_reducer` should not be None.")
        self._nodes_aggregator = blocks.NodesToGlobalsAggregator(
            nodes_reducer)

  def _build(self, graph, prev_state):
    """Connects the global block.
    Args:
      graph: A `graphs.GraphsTuple` containing `Tensor`s, whose individual edges
        (if `use_edges` is `True`), individual nodes (if `use_nodes` is True)
        and per graph globals (if `use_globals` is `True`) should be
        concatenable on the last axis.
    Returns:
      An output `graphs.GraphsTuple` with updated globals.
    """
    globals_to_collect = []

    if self._use_edges:
      blocks._validate_graph(graph, (EDGES,), "when use_edges == True")
      globals_to_collect.append(self._edges_aggregator(graph))

    if self._use_nodes:
      blocks._validate_graph(graph, (NODES,), "when use_nodes == True")
      globals_to_collect.append(self._nodes_aggregator(graph))

    if self._use_globals:
      blocks._validate_graph(graph, (GLOBALS,), "when use_globals == True")
      globals_to_collect.append(graph.globals)

    collected_globals = tf.concat(globals_to_collect, axis=-1)
    updated_globals, updated_state = self._global_model(collected_globals, prev_state)
    return graph.replace(globals=updated_globals), updated_state


class RecurrentGNN(snt.AbstractModule):
  """Implementation of a Graph Network."""

  def __init__(self,
               edge_model_fn,
               node_model_fn,
               reducer=tf.unsorted_segment_sum,
               name="recurrent_graph_network"):
    """Initializes the GraphNetwork module.
    Args:
      edge_model_fn: A callable that will be passed to EdgeBlock to perform
        per-edge computations. The callable must return a Sonnet module (or
        equivalent; see EdgeBlock for details).

      node_model_fn: A callable that will be passed to NodeBlock to perform
        per-node computations. The callable must return a Sonnet module (or
        equivalent; see NodeBlock for details).
     
      reducer: Reducer to be used by NodeBlock and GlobalBlock to aggregate
        nodes and edges. Defaults to tf.unsorted_segment_sum. This will be
        overridden by the reducers specified in `node_block_opt` and
        `global_block_opt`, if any.

      name: The module name.
    """
    super(RecurrentGNN, self).__init__(name=name)
    
    with self._enter_variable_scope():
        self._edge_block = RecurrentEdgeBlock(
            edge_model_fn=edge_model_fn,
            use_edges=True,
            use_receiver_nodes=True,
            use_sender_nodes=True,
            use_globals=True)
        self._node_block = RecurrentNodeBlock(
            node_model_fn=node_model_fn,
            use_received_edges=True,
            use_sent_edges=False,
            use_nodes=True,
            use_globals=True,
            received_edges_reducer=reducer)

  def _build(self, graph):
    """Connects the GraphNetwork.
    Args:
      graph: A `graphs.GraphsTuple` containing `Tensor`s. Depending on the block
        options, `graph` may contain `None` fields; but with the default
        configuration, no `None` field is allowed. Moreover, when using the
        default configuration, the features of each nodes, edges and globals of
        `graph` should be concatenable on the last dimension.
    Returns:
      An output `graphs.GraphsTuple` with updated edges, and nodes.
    """

    return self._node_block(self._edge_block(graph))
