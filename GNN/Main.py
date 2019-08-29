import graph_nets as gn
from graph_nets import utils_np, utils_tf, modules
import networkx as nx
import sonnet as snt
import numpy as np
import pandas as pd
from datetime import datetime
import tensorflow as tf
from matplotlib import pyplot as plt
import GNN
from TripRecords import TripRecords
import wide_n_deep

def plot_graphs_tuple_np(graphs_tuple):
  networkx_graphs = utils_np.graphs_tuple_to_networkxs(graphs_tuple)
  num_graphs = len(networkx_graphs)
  _, axes = plt.subplots(1, num_graphs, figsize=(5*num_graphs, 5))
  if num_graphs == 1:
    axes = axes,
  for graph, ax in zip(networkx_graphs, axes):
    plot_graph_networkx(graph, ax)

def plot_graph_networkx(graph, ax, pos=None):
  node_labels = {node: "{:.3g}".format(data["features"][0])
                 for node, data in graph.nodes(data=True)
                 if data["features"] is not None}
  edge_labels = {(sender, receiver): "{:.3g}".format(data["features"][0])
                 for sender, receiver, data in graph.edges(data=True)
                 if data["features"] is not None}
  global_label = ("{:.3g}".format(graph.graph["features"][0])
                  if graph.graph["features"] is not None else None)

  if pos is None:
    pos = nx.spring_layout(graph)
  nx.draw_networkx(graph, pos, ax=ax, labels=node_labels)

  if edge_labels:
    nx.draw_networkx_edge_labels(graph, pos, edge_labels, ax=ax)

  if global_label:
    plt.text(0.05, 0.95, global_label, transform=ax.transAxes)

  ax.yaxis.set_visible(False)
  ax.xaxis.set_visible(False)
  return pos

def build_model():
    tr = TripRecords()
    #node_model = wide_n_deep.dnn_linear_combined_model()

    graphs_tuple = utils_tf.data_dicts_to_graphs_tuple(tr.graph_tf())
    hidden_units = [100, 75, 50, 25, 1]

    # Create the graph network.
    graph_net_module = GNN.GNN(
        edge_model_fn=lambda: snt.nets.MLP(hidden_units),
        node_model_fn=lambda: snt.nets.MLP(hidden_units))

    # Pass the input graphs to the graph network, and return the output graphs.

   
    output_graphs = graph_net_module(graphs_tuple)
    with tf.Session() as sess:
        output_graphs = sess.run(output_graphs)

    print("Output edges size: {}".format(output_graphs.edges.shape[-1]))
    print("Output nodes size: {}".format(output_graphs.nodes.shape[-1]))
    print("Output globals size: {}".format(output_graphs.globals.shape[-1]))

    return output_graphs

def main(unused_argv):
    build_model()

if __name__ == '__main__':
  tf.app.run()
#first_graph = utils_tf.get_graph(graphs_tuple, 0)
#with tf.Session() as sess:
#    output = sess.run(output_graphs)
#plot_graphs_tuple_np(output_graphs)

#graph_network = GNN(
#    edge_model_fn=lambda: snt.GRU(edge_feature_size * batch_size * 7),
#    node_model_fn=lambda: snt.GRU(node_feature_size * batch_size * 7)
#    )

#output_graphs = graph_network(graphs_tuple)
#plot_graphs_tuple_np(graphs_tuple)
##slices = tf.data.Dataset.from_tensor_slices(reshaped)


