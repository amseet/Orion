///Source: https://bl.ocks.org/cmgiven/a0f58034cea5331a814b30b74aacb8af

function TriangularScatterPlot() {
	var id,
		width ,
		height,	
		margin,
		variable, // value in data that will dictate proportions on chart
		category, // compare data by
		padAngle // effectively dictates the gap between slices
		;

	function tsp(selection) {
		var side = height * 2 / Math.sqrt(3);

		selection.each(function (data) {
			var sideScale = d3.scaleLinear()
				.domain([0, 1])
				.range([0, side]);

			var perpScale = d3.scaleLinear()
				.domain([0, 1])
				.range([height, 0]);

			var r = d3.scaleSqrt()
				.domain([0, d3.max(data, function (d) { return d.total; })])
				.range([0, 10]);

			var colorBlueScale = d3.scaleSequential(d3.interpolateLab("white", "steelblue"))	// Blue color scheme
				.domain([0.333, 1]);
			var colorYellowScale = d3.scaleSequential(d3.interpolateLab("white", "yellow"))	// Yellow color scheme
				.domain([0.333, 1]); 
			var colorRedScale = d3.scaleSequential(d3.interpolateLab("white", "red"))	// Red color scheme
				.domain([0.333, 1]); 

			// set SVG element
			var svg = selection.append('svg')
				.attr('width', width)
				.attr('height', height + margin.top + margin.bottom)
				.append('g')
				.attr('transform', 'translate(' + ((width - side) / 2) + ',' + (margin.top + 0.5) + ')')
				.append('g');

			/// Setup brushing
			var brush = svg.append("g")
				.attr("class", "brush")
				.call(d3.brush()
					.extent([[sideScale(0) - 25, perpScale(1) - 25], [sideScale(1) + 25, perpScale(0) + 25]])
					.on("start brush end", brushed));


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
				.style("fill", function (d, i) {
					if (d.P > d.N && d.P > d.U)
						return colorBlueScale(d.P);
					else if (d.N > d.P && d.N > d.U)
						return colorRedScale(d.N);
					return colorYellowScale(d.U);
				})
				.attr('class', 'point')
				.attr('r', function (d) { return r(d.total); })
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
					return selection[0][0] <= sideScale(d.x) && sideScale(d.x) < selection[1][0]
						&& selection[0][1] <= perpScale(d.y) && perpScale(d.y) < selection[1][1];
				});
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

	return tsp;
}


d3.dsv('\t', '/res/SanDiskSDCard_Sentiments.tsv', function (d) {
	var P = +d.Pos;
	var N = +d.Neg;
	var U = +d.Nut;
	var total = +d.Total;
	var pShare = P;
	var nShare = N;
	var uShare = U;

	return {
		User_Id: d.User_Id,
		total: total,
		P: P,
		N: N,
		U: U,
		pShare: pShare,
		nShare: nShare,
		uShare: uShare,
		x: nShare + (uShare * 0.5),
		y: uShare
	};
}).then(function (data) {
	var margin = { top: 50, bottom: 200 };

	var sentimentTriangle = TriangularScatterPlot()
		.width(960)
		.height(800 - margin.top - margin.bottom)
		.margin(margin);

	d3.select('body')
		.datum(data)
		.call(sentimentTriangle);	
});
