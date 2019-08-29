import category_encoders as ce
import numpy as np
import pandas as pd
from datetime import datetime
import tensorflow as tf
import os
import sonnet as snt

#node_df = pd.read_csv("C:/Users/seetam/Documents/Orion/nodes2015.csv", parse_dates=['Date'],
#                 date_parser=lambda col: pd.to_datetime(col, format='%m/%d/%Y %H:%M')) 

_HASH_BUCKET_SIZE = 1000

class TripRecords:
    def __init__(self, *args, **kwargs):
        self.edge_feature_size = 2
        self.node_feature_size = 7
        self.global_feature_size = 6
        self.datasize = 0
        self.batch_size = 9  # 9 periods/day
        self.saved_nodes_np = "C:/Users/seetam/Documents/Orion/train/saved_nodes_np.npy"
        self.saved_globals_np = "C:/Users/seetam/Documents/Orion/train/saved_globals_np.npy"
        self.saved_nodes_tf = "C:/Users/seetam/Documents/Orion/train/saved_nodes_tf.npy"
        self.saved_globals_tf = "C:/Users/seetam/Documents/Orion/train/saved_globals_tf.npy"
    
    def _preprocess_globals(self, globals_df):
        ### Setup ##
        Period_feature_column = tf.feature_column.categorical_column_with_vocabulary_list(
            key='Period',
            vocabulary_list=['Night','EarlyMorning','MorningRush','Morning','Lunch','Afternoon','AfternoonRush','Evening','Midnight'],
            default_value = 0)
        Period_feature_column = tf.feature_column.indicator_column(Period_feature_column)

        global_columns = [
            tf.feature_column.numeric_column('Month'),
            tf.feature_column.numeric_column('DayofWeek'),
            tf.feature_column.numeric_column('TempAvg'),
            tf.feature_column.numeric_column('Precipitation'),
            tf.feature_column.numeric_column('Snow'),
            Period_feature_column
        ]

        global_features = {
            'Month' : globals_df['Month'],
            'DayofWeek' : globals_df['DayofWeek'],
            'TempAvg' : globals_df['TempAvg'],
            'Precipitation' : globals_df['Precipitation'],
            'Snow' : globals_df['Snow'],
            'Period' : globals_df['Period']
            }
        inputs = tf.feature_column.input_layer(global_features, global_columns)
        var_init = tf.global_variables_initializer()
        table_init = tf.tables_initializer()
        sess = tf.Session()
        sess.run((var_init, table_init))

        print('\n--Preprocessing Nodes--')
        print('Globals shape before: {0}, dtype: {1}'.format(globals_df.shape, globals_df.dtypes))       
        global_tr = sess.run(inputs)
        self.global_feature_size = global_tr.shape[1]
        print('Globals shape after: {0}, dtype: {1}'.format(global_tr.shape, global_tr.dtype))

        return global_tr
    
    def _preprocess_nodes(self, nodes_df):
        ### Setup ###
        labels = nodes_df['OutFlow']
        nodes_df = nodes_df.drop(['OutFlow'], axis=1)

        Landuse_feature_column = tf.feature_column.categorical_column_with_vocabulary_list(
            key='LandUse',
            vocabulary_list=["Other", "Airport", "ResidenceLarge","ResidenceMedium","ResidenceSmall","CommercialLarge", "CommercialMedium", "Mixed"],
            default_value = 0)
        Landuse_feature_column = tf.feature_column.indicator_column(Landuse_feature_column)

        node_columns = [
            tf.feature_column.numeric_column('Population'),
            tf.feature_column.numeric_column('MedianIncome'),
            tf.feature_column.numeric_column('PopDensity'),
            tf.feature_column.numeric_column('BachelorHigher'),
            tf.feature_column.numeric_column('Rating'),
            tf.feature_column.numeric_column('Places'),
            tf.feature_column.numeric_column('Popularity'),
            Landuse_feature_column
        ]

        node_features = {
            'LandUse' : nodes_df['LandUse'],
            'Population' : nodes_df['Population'],
            'MedianIncome' : nodes_df['MedianIncome'],
            'PopDensity' : nodes_df['PopDensity'],
            'BachelorHigher' : nodes_df['BachelorHigher'],
            'Rating' : nodes_df['Rating'],
            'Places' : nodes_df['Places'],
            'Popularity' : nodes_df['Popularity'],
            }

        inputs = tf.feature_column.input_layer(node_features, node_columns)
        var_init = tf.global_variables_initializer()
        table_init = tf.tables_initializer()
        sess = tf.Session()
        sess.run((var_init, table_init))

        print('\n--Preprocessing Nodes--')
        print('Nodes shape before: {0}, dtype: {1}'.format(nodes_df.shape, nodes_df.dtypes))
        nodes_tr = sess.run(inputs)
        self.node_feature_size = nodes_tr.shape[1]
        nodes_tr = nodes_tr.reshape(self.datasize, 2163, self.node_feature_size)
        print('Nodes shape after: {0}, dtype: {1}'.format(nodes_tr.shape, nodes_tr.dtype))

        return nodes_tr, labels

    def graph_np(self):
        data_dict_list = []
        # Read data from file
        if os.path.isfile(self.saved_globals_np) :
            _globals = np.load(self.saved_globals_np, allow_pickle=True)
        else:
            globals_df = pd.read_csv("C:/Users/seetam/Documents/Orion/train/globals.csv")
            _globals = np.array(globals_df)#self._preprocess_globals(globals_df)
            np.save(self.saved_globals_np, _globals)

        self.datasize = _globals.shape[0]

        if os.path.isfile(self.saved_nodes_np) :
            nodes = np.load(self.saved_nodes_np, allow_pickle=True)
        else:
            nodes_df = pd.read_csv("C:/Users/seetam/Documents/Orion/train/nodes.csv")
            nodes_df = nodes_df.drop(['ID'], axis=1)
            nodes = np.array(nodes_df)#self._preprocess_nodes(nodes_df)
            np.save(self.saved_nodes_np, nodes)

        # loop through data elements
        for i in range(2):
            print('processing dict {0}'.format(i))
            edges = pd.read_csv("C:/Users/seetam/Documents/Orion/train/Edges/edges_{0}.csv".format(i))

            _global = _globals[i]
            _nodes = nodes[i]
            _senders = np.array(edges['SenderID'], dtype = np.float32)
            _receivers = np.array(edges['ReceiverID'], dtype = np.float32)
            _edges = np.array(edges.drop(['SenderID', 'ReceiverID'], axis=1), dtype = np.float32)

            data_dict = {
                "globals": _global,
                "nodes": _nodes,
                "edges": _edges,
                "senders": _senders,
                "receivers": _receivers
            }
            data_dict_list.append(data_dict)

        return data_dict_list

    def graph_tf(self):
        data_dict_list = []
        # Read data from file
        if os.path.isfile(self.saved_globals_tf) :
            _globals = np.load(self.saved_globals_tf, allow_pickle=True)
        else:
            globals_df = pd.read_csv("C:/Users/seetam/Documents/Orion/train/globals.csv")
            _globals = self._preprocess_globals(globals_df)
            np.save(self.saved_globals_tf, _globals)

        self.datasize = _globals.shape[0]

        if os.path.isfile(self.saved_nodes_tf) :
            nodes = np.load(self.saved_nodes_tf, allow_pickle=True)
        else:
            nodes_df = pd.read_csv("C:/Users/seetam/Documents/Orion/train/nodes.csv")
            nodes_df = nodes_df.drop(['ID'], axis=1)
            nodes, node_labels = self._preprocess_nodes(nodes_df)
            np.save(self.saved_nodes_tf, nodes)

        # loop through data elements
        for i in range(2):
            print('processing dict {0}'.format(i))
            edges = pd.read_csv("C:/Users/seetam/Documents/Orion/train/Edges/edges_{0}.csv".format(i))

            _global = _globals[i]
            _nodes = nodes[i]
            _node_labels = np.array(node_labels[i], dtype = np.float32)
            _senders = np.array(edges['SenderID'], dtype = np.float32)
            _receivers = np.array(edges['ReceiverID'], dtype = np.float32)
            _edge_labels = np.array(edges['InFlow'], dtype = np.float32)
            _edges = np.array(edges.drop(['SenderID', 'ReceiverID', 'InFlow'], axis=1), dtype = np.float32)

            data_dict = {
                "globals": _global,
                "nodes": _nodes,
                "node_labels": _node_labels,
                "edges": _edges,
                "edge_labels": _edge_labels,
                "senders": _senders,
                "receivers": _receivers
            }
            data_dict_list.append(data_dict)

        return data_dict_list

    def build_node_columns(self):
        """Builds a set of wide and deep feature columns."""         
        # Continuous variable columns
        population = tf.feature_column.numeric_column('Population')
        income = tf.feature_column.numeric_column('MedianIncome')
        popdensity = tf.feature_column.numeric_column('PopDensity')
        education = tf.feature_column.numeric_column('BachelorHigher')
        popularity = tf.feature_column.numeric_column('Popularity')
        rating = tf.feature_column.numeric_column('Rating')
        places = tf.feature_column.numeric_column('Places')

        # Categorical variable columns
        Landuse_feature_column = tf.feature_column.categorical_column_with_vocabulary_list(
            key='LandUse',
            vocabulary_list=["Other", "Airport", "ResidenceLarge","ResidenceMedium","ResidenceSmall",
                             "CommercialLarge", "CommercialMedium", "Mixed"],
            default_value = 0)

        # Transformations.
        income_buckets = tf.feature_column.bucketized_column(
            income, boundaries=[10000, 15000, 25000, 35000, 50000, 75000, 100000, 150000, 200000])
        rating_buckets = tf.feature_column.bucketized_column(
            rating, boundaries=[1, 2, 3, 4])
        places_buckets = tf.feature_column.bucketized_column(
            places, boundaries=[5, 10, 15, 20])
        popularity_buckets = tf.feature_column.categorical_column_with_hash_bucket(
            'Popularity', hash_bucket_size=100)
        # Wide columns and deep columns.
        base_columns = [
            Landuse_feature_column, income_buckets, rating_buckets, places_buckets, popularity_buckets
        ]

        crossed_columns = [
            tf.feature_column.crossed_column(
                [popularity_buckets, rating_buckets, places_buckets], hash_bucket_size=1000),
            tf.feature_column.crossed_column(
                [income_buckets, 'Education'], hash_bucket_size=100),
        ]

        wide_columns = base_columns + crossed_columns

        deep_columns = [
            population,
            income,
            popdensity,
            education,
            popularity,
            rating,
            places,
            tf.feature_column.embedding_column(Landuse_feature_column, dimension=Landuse_feature_column.num_buckets),
        ]

        return wide_columns, deep_columns

    def build_edge_columns(self):
        """Builds a set of wide and deep feature columns."""         
        # Continuous variable columns
        distance = tf.feature_column.numeric_column('Distance')
   
        # Wide columns and deep columns.
        wide_columns = []

        deep_columns = [distance]

        return wide_columns, deep_columns

    def build_global_columns(self):
        """Builds a set of wide and deep feature columns."""         
        # Continuous variable columns
        tempavg = tf.feature_column.numeric_column('TempAvg'),
        precipitation = tf.feature_column.numeric_column('Precipitation')
        snow = tf.feature_column.numeric_column('Snow'),
   
        # Categorical identity columns
        month_feature_column = tf.feature_column.categorical_column_with_identity(
            key='Month',
            num_buckets=12)
        dayofweek_feature_column = tf.feature_column.categorical_column_with_identity(
            key='DayofWeek',
            num_buckets=7)

        # Categorical variable columns
        Period_feature_column = tf.feature_column.categorical_column_with_vocabulary_list(
            key='Period',
            vocabulary_list=['Night', 'EarlyMorning', 'MorningRush', 'Morning', 'Lunch', 'Afternoon', 'AfternoonRush', 'Evening', 'Midnight'],
            default_value = 0)

        # Wide columns and deep columns.
        base_columns = [month_feature_column, dayofweek_feature_column, Period_feature_column]

        crossed_columns = [tf.feature_column.crossed_column(
                ['TempAvg', 'Precipitation'], hash_bucket_size=100),
                tf.feature_column.crossed_column(
                ['TempAvg', 'Snow'], hash_bucket_size=100)]

        wide_columns = base_columns + crossed_columns

        deep_columns = [
            tempavg, 
            precipitation, 
            snow,
            tf.feature_column.embedding_column(Period_feature_column, dimension=Period_feature_column.num_buckets),
            tf.feature_column.indicator_column(month_feature_column),
            tf.feature_column.indicator_column(dayofweek_feature_column)]

        return wide_columns, deep_columns

    def node_feature_dict(self, nodes_df):
        return {
            'LandUse' : nodes_df['LandUse'],
            'Population' : nodes_df['Population'],
            'MedianIncome' : nodes_df['MedianIncome'],
            'PopDensity' : nodes_df['PopDensity'],
            'BachelorHigher' : nodes_df['BachelorHigher'],
            'Rating' : nodes_df['Rating'],
            'Popularity' : nodes_df['Popularity'],
            'Places' : nodes_df['Places']
            }

    def global_feature_dict(self, globals_df):
        return {
            'Month' : globals_df['Month'],
            'DayofWeek' : globals_df['DayofWeek'],
            'TempAvg' : globals_df['TempAvg'],
            'Precipitation' : globals_df['Precipitation'],
            'Snow' : globals_df['Snow'],
            'Period' : globals_df['Period']
            }

    def edge_feature_dict(self, edges_df):
        return {
            'Distance' : edges_df['Distance']
            }

    def build_model(self, column_fn):
        wide_columns, deep_columns = column_fn
        hidden_units = [100, 75, 50, 25, 1]

        estimator = tf.estimator.DNNLinearCombinedRegressor(
            # wide settings
            linear_feature_columns= wide_columns,
            linear_optimizer=tf.train.FtrlOptimizer(
                learning_rate=0.1,
                l1_regularization_strength=0.001,
                l2_regularization_strength=0.001),
            # deep settings
            dnn_feature_columns=deep_columns,
            dnn_hidden_units=hidden_units,
            dnn_optimizer=tf.train.ProximalAdagradOptimizer(
                learning_rate=0.1,
                l1_regularization_strength=0.001,
                l2_regularization_strength=0.001),
            # warm-start settings
            #warm_start_from="/path/to/checkpoint/dir"
            )

        return estimator