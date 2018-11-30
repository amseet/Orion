d3.tsv("/res/data.tsv", function (data) {
    console.log(data["Comment"]);
});

var globalWidth = 1280;
var globalHeight = 720;

var feature_donut = donutChart()
    .id("feature_donut")
    .width(1280)
    .height(720)
    .cornerRadius(3) // sets how rounded the corners are on each slice
    .padAngle(0.015) // effectively dictates the gap between slices
    .variable('count')
    .category('name');

var sentiment_donut = donutChart()
    .id("sentiment_donut")
    .width(300)
    .height(150)
    .cornerRadius(3) // sets how rounded the corners are on each slice
    .padAngle(0.015) // effectively dictates the gap between slices
    .variable('count')
    .category('name')
    .isAppend(true);

function extractSentiment(aspect) {
    var sentiment = [];
    sentiment[0] = {
        "name": "Positive",
        "count": aspect.pos_count,
        'color': 'green'
    };
    sentiment[1] = {
        "name": "Negative",
        "count": aspect.neg_count,
        "color": "red"
    };
    return sentiment;
}
var global_sentiment = [];
var openFile = function (event) {
    var input = event.target;

    var reader = new FileReader();
    reader.onload = function () {
                
        var json = JSON.parse(reader.result);
        var aspects = [];

        var colors = {
            0: "#5687d1",
            1: "#7b615c",
            2: "#de783b",
            3: "#6ab975",
            4: "#a173d1",
            5: "#bbbbbb"
        };
        var pos_count = 0;
        var neg_count = 0;
        //reformat json into an array
        Object.keys(json.product.flattened_aspects).forEach(function (aspect, i) {
            if (i < 6)
                aspects[i] = {
                    "color":colors[i],
                    "name": json.product.flattened_aspects[aspect].aspect,
                    "pos_count": json.product.flattened_aspects[aspect].pos_count,
                    "neg_count": json.product.flattened_aspects[aspect].neg_count,
                    "count": json.product.flattened_aspects[aspect].pos_count + json.product.flattened_aspects[aspect].neg_count
                };
            pos_count += json.product.flattened_aspects[aspect].pos_count;
            neg_count += json.product.flattened_aspects[aspect].neg_count;
        });

        global_sentiment[0] = {
            "name": "Positive",
            "count": pos_count,
            'color': 'green'
        };
        global_sentiment[1] = {
            "name": "Negative",
            "count": neg_count,
            "color": "red"
        };

        d3.select('#chart')
            .datum(aspects) // bind data to the div
            .call(feature_donut); // draw chart in div
        d3.select('#feature_donut')
            .datum(global_sentiment) // bind data to the div
            .call(sentiment_donut); // draw chart in div


    };
    reader.readAsText(input.files[0]);
};

function donutChart() {
    var id,
        width,
        height,
        margin = { top: 10, right: 10, bottom: 10, left: 10 },
        colour = d3.scaleOrdinal(d3.schemeCategory20c), // colour scheme
        variable, // value in data that will dictate proportions on chart
        category, // compare data by
        padAngle, // effectively dictates the gap between slices
        floatFormat = d3.format('.4r'),
        cornerRadius, // sets how rounded the corners are on each slice
        percentFormat = d3.format(',.2%'),
        isAppend = false;

    function chart(selection) {
        selection.each(function (data) {
            // generate chart

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
                .outerRadius(radius * 0.8)
                .innerRadius(radius * 0.6)
                .cornerRadius(cornerRadius)
                .padAngle(padAngle);

            // this arc is used for aligning the text labels
            var outerArc = d3.arc()
                .outerRadius(radius * 0.9)
                .innerRadius(radius * 0.9);
            // ===========================================================================================

            // ===========================================================================================
            // append the svg object to the selection
            var svg;
            if (isAppend === true) {
                svg = d3.select(this)
                    .append('g')
                    .attr('id',id)
                    .attr('transform', 'translate(' + globalWidth / 2 + ',' + globalHeight / 2 + ')');
            }
            else {
                svg = selection.append('svg')
                    .attr('id', id)
                    .attr('width', width + margin.left + margin.right)
                    .attr('height', height + margin.top + margin.bottom)
                    .append('g')
                    .attr('transform', 'translate(' + globalWidth / 2 + ',' + globalHeight / 2 + ')');
            }

            // ===========================================================================================

            // ===========================================================================================
            // g elements to keep elements within svg modular
            svg.append('g').attr('class', 'slices');
            svg.append('g').attr('class', 'labelName');
            svg.append('g').attr('class', 'lines');
            // ===========================================================================================

            // ===========================================================================================
            // add and colour the donut slices
            var path = svg.select('.slices')
                .datum(data).selectAll('path')
                .data(pie)
                .enter().append('path')
                .attr('fill', function (d) { return d.data['color']; })
                .attr('d', arc);
            // ===========================================================================================

            // ===========================================================================================
            // add text labels
            var label = svg.select('.labelName').selectAll('text')
                .data(pie)
                .enter().append('text')
                .attr('dy', '.35em')
                .html(function (d) {
                    // add "key: value" for given category. Number inside tspan is bolded in stylesheet.
                    return d.data[category] + ': <tspan>' + d.data[variable] + '</tspan>';
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

            // ===========================================================================================
            // add lines connecting labels to slice. A polyline creates straight lines connecting several points
            var polyline = svg.select('.lines')
                .selectAll('polyline')
                .data(pie)
                .enter().append('polyline')
                .attr('points', function (d) {

                    // see label transform function for explanations of these three lines.
                    var pos = outerArc.centroid(d);
                    pos[0] = radius * 0.95 * (midAngle(d) < Math.PI ? 1 : -1);
                    return [arc.centroid(d), outerArc.centroid(d), pos];
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

                    //svg.append('text')
                    //    .attr('class', 'toolCircle')
                    //    .attr('dy', -15) // hard-coded. can adjust this to adjust text vertical alignment in tooltip
                    //    .html(toolTipHTML(data)) // add text to the circle.
                    //    .style('font-size', '.9em')
                    //    .style('text-anchor', 'middle'); // centres text in tooltip

                    //svg.append('circle')
                    //    .attr('class', 'toolCircle')
                    //    .attr('r', radius * 0.55) // radius of tooltip circle
                    //    .style('fill', data.data['color']) // colour based on category mouse is over
                    //    .style('fill-opacity', 0.35);
                    var f = extractSentiment(data.data);
                    d3.selectAll('#sentiment_donut').remove();
                    d3.select('#feature_donut')
                        .datum(f) // bind data to the div
                        .call(sentiment_donut); // draw chart in div

                });

                // remove the tooltip when mouse leaves the slice/label
                selection.on('mouseout', function () {
                    d3.selectAll('.toolCircle').remove();
                    d3.selectAll('path')
                        .style("opacity", 1);
                    d3.selectAll('#sentiment_donut').remove();
                    d3.select('#feature_donut')
                        .datum(global_sentiment) // bind data to the div
                        .call(sentiment_donut); // draw chart in div
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

    return chart;
}
