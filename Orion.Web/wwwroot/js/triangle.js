/**	Class Definitions */
let TriPoint = class {
	constructor() {
		this.x = 0;
		this.y = 0;
	}
};
let Feature = class {
	constructor(name, enabled) {
		this.Name = name;
		this.P = 0;			// count of Positive sentiment per feature
		this.N = 0;			// count of Negative sentiment per feature
		this.U = 0;			// count of Neutral sentiment per feature
		this.Total = 0;		// Total count of sentiment per feature
		this.isEnabled = enabled;
	}
	Add(sentiment /*Type String*/) {
		if (sentiment === "Positive")
			this.P++;
		else if (sentiment === "Negative")
			this.N++;
		else if (sentiment === "Neutral")
			this.U++;
		else
			throw "Error in Feature.Add - Unknown Sentiment '" + sentiment + "'.";

		this.Total++;
	}
};

let User = class {
	constructor(userId, enabled) {
		this.User_Id = userId;
		this.P = 0;			// aggrigated Positive sentiment per User
		this.N = 0;			// aggrigated Negative sentiment per User
		this.U = 0;			// aggrigated Neutral sentiment per User
		this.Total = 0;		// Total count of sentiment per User
		this.isEnabled = enabled;
	}
	Add(P, N, U) {
		if (isNaN(P) || isNaN(N) || isNaN(U))
			throw "Error in User.Add - value is NaN " + P + " " + N + " " + U;
		this.P += +P;
		this.N += +N;
		this.U += +U;
		this.Total++;
	}
};

let ReviewData = class {
	constructor() {
		this.Rows = [];
	}

	Add(row) {
		this.Rows[+row.Uid] = row;
		this.Rows[+row.Uid].isEnabled = true;
	}

	GetFeatures() {
		var Features = [];
		for (var i in this.Rows) {
			var row = this.Rows[i];
			if (row.Feature !== undefined && row.isEnabled === true) {
				if (Features[row.Feature] === undefined)
					Features[row.Feature] = new Feature(row.Feature, row.isEnabled);
				Features[row.Feature].Add(row.Sentiment);
			}
		}
		return Features;
	}

	SetByFeature(featureName, enabled) {
		for (var i in this.Rows) {
			if (this.Rows[i].Feature !== undefined && this.Rows[i].Feature === featureName)
				this.Rows[i].isEnabled = enabled;
		}
	}

	SetByUserId(UserId, enabled) {
		for (var i in this.Rows) {
			if (this.Rows[i].User_Id !== undefined && this.Rows[i].User_Id === UserId)
				this.Rows[i].isEnabled = enabled;
		}
	}

	GetUsers() {
		var Users = [];
		for (var i in this.Rows) {
			var row = this.Rows[i];
			if (row.User_Id !== undefined) {
				if (Users[row.User_Id] === undefined)
					Users[row.User_Id] = new User(row.User_Id, row.isEnabled);
				Users[row.User_Id].Add(row.Pos, row.Neg, row.Nut);
				Users[row.User_Id].isEnabled = Users[row.User_Id].isEnabled || row.isEnabled;
			}
		}
		return Users;
	}
};

//Extension Methods
var toArray = function (a) {
	return Object.values(a);
};
/* *************************************** */

var globalWidth = 1280;
var globalHeight = 720;

///Source: https://bl.ocks.org/cmgiven/a0f58034cea5331a814b30b74aacb8af
function TriangularScatterPlot() {
	var id,
		width,
		height,
		margin,
		variable, // value in data that will dictate proportions on chart
		category, // compare data by
		padAngle // effectively dictates the gap between slices
	isAppend = false;

	function tsp(selection) {
		var side = height * 2 / Math.sqrt(3);

		selection.each(function (data) {
			//preprocess the data
			for (var i in data) {
				var elm = data[i];
				if (elm !== undefined) {
					elm.sum = elm.P + elm.N + elm.U;
					elm.pShare = elm.P / elm.sum;
					elm.nShare = elm.N / elm.sum;
					elm.uShare = elm.U / elm.sum;
					elm.x = elm.nShare + (elm.uShare * 0.5);
					elm.y = elm.uShare;
				}
			}

			var sideScale = d3.scaleLinear()
				.domain([0, 1])
				.range([0, side]);

			var perpScale = d3.scaleLinear()
				.domain([0, 1])
				.range([height, 0]);

			var r = d3.scaleSqrt()
				.domain([0, d3.max(data, function (d) { return d.Total; })])
				.range([0, 10]);

			var colorBlueScale = d3.scaleSequential(d3.interpolateLab("white", "steelblue"))	// Blue color scheme
				.domain([0.333, 1]);
			var colorYellowScale = d3.scaleSequential(d3.interpolateLab("white", "yellow"))	// Yellow color scheme
				.domain([0.333, 1]);
			var colorRedScale = d3.scaleSequential(d3.interpolateLab("white", "red"))	// Red color scheme
				.domain([0.333, 1]);

			// set SVG element
			var svg;

			if (isAppend === true) {
				svg = d3.select(this)
					.append('g')
					.attr('id', 'g_'+id)
					.attr('width', width)
					.attr('height', height)
					.append('g')
					.attr('transform', 'translate(' + ((globalWidth - side) / 2) + ',' + ((globalHeight - side) / 2) + ')')
					.append('g');
			}
			else {
				svg = selection.append('svg')
					.attr('id', id)
					.attr('width', width)
					.attr('height', height + margin.top + margin.bottom)
					.append('g')
					.attr('id', 'g_'+id)
					.attr('transform', 'translate(' + ((width - side) / 2) + ',' + (margin.top + 0.5) + ')')
					.append('g');
			}
			/// Setup brushing
			var offset = 25;
			var brush = svg.append("g")
				.attr("class", "brush")
				.call(d3.brush()
					.extent([[sideScale(0) - offset, perpScale(1) - offset], [sideScale(1) + offset, perpScale(0) + offset]])
					.on("brush", brushed)
					.on("end", endBrushed));


			// triangle Y axis
			var axis = d3.axisLeft()
				.scale(perpScale)
				.tickSize(0)
				.tickFormat(function (n) { return ""; });

			//build the triangle line-by-line
			var axes = svg.selectAll('.axis')
				.data(['P', 'N', 'U'])
				.enter()
				.append('g')
				.attr('class', function (d) { return 'axis ' + d; })
				.attr('transform', function (d) {
					return d === 'U' ? ''
						: 'rotate(' + (d === 'P' ? 240 : 120) + ',' + (side * 0.5) + ',' + (height / 3 * 2) + ')';
				})
				.call(axis);

			axes.selectAll('.tick')
				.append('line')
				.attr('class', 'grid')
				.attr('x1', function (d) { return side * (d * 0.5); })
				.attr('x2', function (d) { return side * (-d * 0.5 + 1); })
				.attr('y1', 0)
				.attr('y2', 0);

			axes.append('text')
				.attr('class', 'label')
				.attr('x', side * 0.5)
				.attr('y', -6)
				.attr('text-anchor', 'middle')
				.attr('letter-spacing', '-8px')
				.text(function (d) { return d; });

			// plot the points
			var points = svg.selectAll('.point')
				.data(data)
				.enter().append('circle')
				.style("fill", function (d) {
					if (d.P > d.N && d.P > d.U)
						return colorBlueScale(d.pShare);
					else if (d.N > d.P && d.N > d.U)
						return colorRedScale(d.nShare);
					return colorYellowScale(d.uShare);
				})
				.attr('class', 'point')
				.attr('r', function (d) { return r(d.Total); })
				.attr('cx', function (d) { return sideScale(d.x); })
				.attr('cy', function (d) { return perpScale(d.y); })
				.on('click', function (d) {
					console.log(d);
				});

			points.append('title')
				.text(function (d) { return d.User_Id; });

			function brushed() {
				var selection = d3.event.selection;
				points.classed("selected", selection && function (d) {
					d.isEnabled = selection[0][0] <= sideScale(d.x) && sideScale(d.x) < selection[1][0]
						&& selection[0][1] <= perpScale(d.y) && perpScale(d.y) < selection[1][1];
					return d.isEnabled;
				});
			}

			function endBrushed() {
				for (var i in data) {
					var d = data[i];
					reviews.SetByUserId(d.User_Id, d.isEnabled);
				}

				// Select the section we want to apply our changes to
				//var svg = d3.select("#feature_donut").transition();

				//// Make the changes
				//svg.select(".slices")   // change the line
				//	.duration(750)
				//	.attr("d", valueline(data));
				//svg.select(".labelNames") // change the x axis
				//	.duration(750)
				//	.call(xAxis);
				//svg.select(".lines") // change the y axis
				//	.duration(750)
				//	.call(yAxis);


				d3.selectAll('#g_feature_donut').remove();
				d3.select('#feature_donut')
					.datum(toArray(reviews.GetFeatures()))
					.call(feature_donut);
			}
		});

	}

	tsp.id = function (value) {
		if (!arguments.length) return id;
		id = value;
		return tsp;
	};

	tsp.width = function (value) {
		if (!arguments.length) return width;
		width = value;
		return tsp;
	};

	tsp.height = function (value) {
		if (!arguments.length) return height;
		height = value;
		return tsp;
	};

	tsp.margin = function (value) {
		if (!arguments.length) return margin;
		margin = value;
		return tsp;
	};

	tsp.color = function (value) {
		if (!arguments.length) return color;
		color = value;
		return tsp;
	};

	tsp.variable = function (value) {
		if (!arguments.length) return variable;
		variable = value;
		return tsp;
	};

	tsp.category = function (value) {
		if (!arguments.length) return category;
		category = value;
		return tsp;
	};

	tsp.padAngle = function (value) {
		if (!arguments.length) return padAngle;
		padAngle = value;
		return tsp;
	};

	tsp.isAppend = function (value) {
		if (!arguments.length) return isAppend;
		isAppend = value;
		return tsp;
	};

	tsp.clear = function () {
		d3.selectAll('#' + id).remove();
	};

	return tsp;
}

function DonutChart() {
	var id,
		width,
		height,
		margin = { top: 10, right: 10, bottom: 10, left: 10 },
		colour = d3.scaleOrdinal(d3.schemePaired), // colour scheme
		variable, // value in data that will dictate proportions on chart
		category, // compare data by
		padAngle, // effectively dictates the gap between slices
		cornerRadius, // sets how rounded the corners are on each slice
		isAppend = false;

	var colorBlueScale = d3.scaleSequential(d3.interpolateLab("white", "steelblue"))	// Blue color scheme
		.domain([0.333, 1]);
	var colorYellowScale = d3.scaleSequential(d3.interpolateLab("white", "yellow"))	// Yellow color scheme
		.domain([0.333, 1]);
	var colorRedScale = d3.scaleSequential(d3.interpolateLab("white", "red"))	// Red color scheme
		.domain([0.333, 1]);

	function chart(selection) {
		selection.each(function (data) {
			data = data.slice(0, 10);

			// generate chart
			var nodeData = { "name": "Features", "children": [] };
			for (var i in data) {
				var d = data[i];
				nodeData.children[i] = {
					"name": d.Name,
					"children": [
						{ "name": "Positive", "size": +d.P },
						{ "name": "Negative", "size": +d.N },
						{ "name": "Neutral", "size": +d.U }
					],
					"row": d
				};
			}

			// ===========================================================================================
			// Set up constructors for making donut. See https://github.com/d3/d3-shape/blob/master/README.md
			var radius = Math.min(width, height) / 2;

			// creates a new pie generator
			var pie = d3.pie()
				.value(function (d) { return d[variable]; })
				.sort(null);

			// contructs and arc generator. This will be used for the donut. The difference between outer and inner
			// radius will dictate the thickness of the donut
			var arc = d3.arc()
				.startAngle(function (d) { return d.x0; })
				.endAngle(function (d) { return d.x1; })
				.innerRadius(function (d) { return (radius * 0.8) + (d.depth - 1) * 25; })
				.outerRadius(function (d) { return (radius * 0.6) + (d.depth - 1) * (radius * 0.2) + 1; })
				.cornerRadius(cornerRadius)
				.padAngle(padAngle);

			//temp arc
			var arc2 = d3.arc()
				.innerRadius(function (d) { return radius * 0.8; })
				.outerRadius(function (d) { return radius * 0.6; })
				.cornerRadius(cornerRadius)
				.padAngle(padAngle);

			// this arc is used for aligning the text labels
			var outerArc = d3.arc()
				.outerRadius(radius * 0.9)
				.innerRadius(radius * 0.9);

			// Data strucure
			var partition = d3.partition()
				.size([2 * Math.PI, radius]);
			// Find data root
			var root = d3.hierarchy(nodeData)
				.sum(function (d) { return d.size });
			// Size arcs
			partition(root);

			// ===========================================================================================
			// append the svg object to the selection
			var svg;
			if (isAppend === true) {
				svg = d3.select(this)
					.append('g')
					.attr('id', id)
					.attr('transform', 'translate(' + globalWidth / 2 + ',' + globalHeight / 2 + ')');
			}
			else {
				svg = selection.append('svg')
					.attr('id', id)
					.attr('width', width + margin.left + margin.right)
					.attr('height', height + margin.top + margin.bottom)
					.append('g')
					.attr('id', "g_" + id)
					.attr('transform', 'translate(' + globalWidth / 2 + ',' + globalHeight / 2 + ')');
			}

			// ===========================================================================================

			// ===========================================================================================
			// g elements to keep elements within svg modular
			svg.append('g').attr('class', 'slices');
			svg.append('g').attr('class', 'labelName');
			svg.append('g').attr('class', 'lines');


			// ===========================================================================================
			// add and colour the donut slices
			var path = svg.select('.slices')
				.selectAll('path')
				.data(root.descendants())
				.enter().append('path')
				.attr("display", function (d) { return d.depth ? null : "none"; })
				.attr('fill', function (d) {
					var depth = d.depth;
					if (depth === 1) {
						var r = d.data.row;
						if (r.P > r.N && r.P > r.U)
							return colorBlueScale(r.P / r.Total);
						else if (r.N > r.P && r.N > r.U)
							return colorRedScale(r.N / r.Total);
						return colorYellowScale(r.U / r.Total);
					}
					else if (depth === 2) {
						if (d.data.name === "Positive")
							return colorBlueScale(0.667);
						else if (d.data.name === "Negative")
							return colorRedScale(0.667);
						return colorYellowScale(0.667);
					}
				})
				.attr('d', arc);

			// ===========================================================================================

			// ===========================================================================================
			// add text labels
			var label = svg.select('.labelName')
				.datum(data).selectAll('text')
				.data(pie)
				.enter().append('text')
				.attr('dy', '.25em')
				.html(function (d) {
					// add "key: value" for given category. Number inside tspan is bolded in stylesheet.
					return '<tspan>' + d.data[category] + '</tspan>'
						+ " - P" + ': <tspan>' + d.data.P + '</tspan>'
						+ ", N" + ': <tspan>' + d.data.N + '</tspan>'
						+ ", U" + ': <tspan>' + d.data.U + '</tspan>'
						+ " - Total" + ': <tspan>' + d.data[variable] + '</tspan>';
				})
				.attr('transform', function (d) {

					// effectively computes the centre of the slice.
					// see https://github.com/d3/d3-shape/blob/master/README.md#arc_centroid
					var pos = outerArc.centroid(d);

					// changes the point to be on left or right depending on where label is.
					pos[0] = radius * 0.95 * (midAngle(d) < Math.PI ? 1 : -1);
					return 'translate(' + pos + ')';
				})
				.style('text-anchor', function (d) {
					// if slice centre is on the left, anchor text to start, otherwise anchor to end
					return (midAngle(d)) < Math.PI ? 'start' : 'end';
				});

			// ===========================================================================================
			// add lines connecting labels to slice. A polyline creates straight lines connecting several points
			var polyline = svg.select('.lines')
				.datum(data).selectAll('polyline')
				.data(pie)
				.enter().append('polyline')
				.attr('points', function (d) {

					// see label transform function for explanations of these three lines.
					var pos = outerArc.centroid(d);
					pos[0] = radius * 0.95 * (midAngle(d) < Math.PI ? 1 : -1);
					return [arc2.centroid(d), outerArc.centroid(d), pos];
				});
			// ===========================================================================================

			// ===========================================================================================
			// add tooltip to mouse events on slices and labels
			d3.selectAll('.labelName text, .slices path').call(toolTip);
			// ===========================================================================================

			// ===========================================================================================
			// Functions

			// calculates the angle for the middle of a slice
			function midAngle(d) { return d.startAngle + (d.endAngle - d.startAngle) / 2; }

			// function that creates and adds the tool tip to a selected element
			function toolTip(selection) {

				// add tooltip (svg circle element) when mouse enters label or slice
				selection.on('mouseenter', function (data, idx) {
					d3.selectAll('path')
						.style("opacity", 0.3);

					// Then highlight only those that are an ancestor of the current segment.
					this.style["opacity"] = 1;
					this.style["stroke"] = "#888";

					//filter by feature
					d3.selectAll('#g_tri').remove();
					d3.select('#feature_donut')
						.datum(toArray(reviews.GetUsers()))
						.call(sentimentTriangle);
				});

				//remove the tooltip when mouse leaves the slice/label
				selection.on('mouseout', function () {				
					d3.selectAll('path')
						.style("opacity", 1)
						.style("stroke", 'none');

					d3.selectAll('#g_tri').remove();
					d3.select('#feature_donut')
						.datum(toArray(reviews.GetUsers()))
						.call(sentimentTriangle);
				});
			}

			// function to create the HTML string for the tool tip. Loops through each key in data object
			// and returns the html string key: value
			function toolTipHTML(data) {

				var tip = '',
					i = 0;

				for (var key in data.data) {

					// if value is a number, format it as a percentage
					var value = /*(!isNaN(parseFloat(data.data[key]))) ? percentFormat(data.data[key]) :*/ data.data[key];

					// leave off 'dy' attr for first tspan so the 'dy' attr on text element works. The 'dy' attr on
					// tspan effectively imitates a line break.
					if (i === 0) tip += '<tspan x="0">' + key + ': ' + value + '</tspan>';
					else tip += '<tspan x="0" dy="1.2em">' + key + ': ' + value + '</tspan>';
					i++;
				}

				return tip;
			}
			// ===========================================================================================

		});
	}

	chart.id = function (value) {
		if (!arguments.length) return id;
		id = value;
		return chart;
	};

	// getter and setter functions. See Mike Bostocks post "Towards Reusable Charts" for a tutorial on how this works.
	chart.width = function (value) {
		if (!arguments.length) return width;
		width = value;
		return chart;
	};

	chart.height = function (value) {
		if (!arguments.length) return height;
		height = value;
		return chart;
	};

	chart.margin = function (value) {
		if (!arguments.length) return margin;
		margin = value;
		return chart;
	};

	chart.radius = function (value) {
		if (!arguments.length) return radius;
		radius = value;
		return chart;
	};

	chart.padAngle = function (value) {
		if (!arguments.length) return padAngle;
		padAngle = value;
		return chart;
	};

	chart.cornerRadius = function (value) {
		if (!arguments.length) return cornerRadius;
		cornerRadius = value;
		return chart;
	};

	chart.colour = function (value) {
		if (!arguments.length) return colour;
		colour = value;
		return chart;
	};

	chart.variable = function (value) {
		if (!arguments.length) return variable;
		variable = value;
		return chart;
	};

	chart.category = function (value) {
		if (!arguments.length) return category;
		category = value;
		return chart;
	};

	chart.isAppend = function (value) {
		if (!arguments.length) return isAppend;
		isAppend = value;
		return chart;
	};

	chart.clear = function () {
		d3.selectAll('#' + id).remove();
	};

	return chart;
}

var margin = { top: 50, bottom: 200 };

var sentimentTriangle = TriangularScatterPlot()
	.id("tri")
	.width(580)
	.height(250)
	.margin(margin)
	.isAppend(true);

var feature_donut = DonutChart()
	.id("feature_donut")
	.width(1280)
	.height(720)
	.cornerRadius(3) // sets how rounded the corners are on each slice
	.padAngle(0.015) // effectively dictates the gap between slices
	.variable('Total')
	.category('Name');

var reviews;
function createVis() {
	reviews = new ReviewData();
	var value = document.getElementById("selection").value;
	var image_path = "/images/" + value + ".jpg";

	var img = document.getElementById("product_img");
	img.src = image_path;

	var path = value + "_Features.tsv";
	d3.dsv('\t', '/res/' + path, function (data) {
		reviews.Add(data);
	}).then(function () {

		d3.selectAll('svg').remove();

		d3.select('#vis')
			.datum(toArray(reviews.GetFeatures()))
			.call(feature_donut);

		d3.select('#feature_donut')
			.datum(toArray(reviews.GetUsers()))
			.call(sentimentTriangle);
	});
}
createVis();
