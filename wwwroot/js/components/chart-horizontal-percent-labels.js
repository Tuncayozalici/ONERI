(function (window) {
    window.OneriChartHelpers = window.OneriChartHelpers || {};

    function formatPercent(value, fractionDigits) {
        var numeric = Number(value);
        if (!Number.isFinite(numeric)) {
            return "";
        }

        var digits = Number.isFinite(fractionDigits) ? fractionDigits : 2;
        return numeric.toLocaleString("tr-TR", {
            minimumFractionDigits: digits,
            maximumFractionDigits: digits
        }) + "%";
    }

    window.OneriChartHelpers.createHorizontalPercentLabelPlugin = function (options) {
        var opts = options || {};
        var textColor = opts.textColor || "#1f2937";
        var insideTextColor = opts.insideTextColor || "#ffffff";
        var font = opts.font || "600 12px Poppins, sans-serif";
        var offset = Number.isFinite(opts.offset) ? opts.offset : 8;
        var outsideOffset = Number.isFinite(opts.outsideOffset) ? opts.outsideOffset : 8;
        var minValue = Number.isFinite(opts.minValue) ? opts.minValue : 0;

        return {
            id: "horizontalPercentLabels",
            afterDatasetsDraw: function (chart) {
                if (!chart || !chart.options || chart.options.indexAxis !== "y") {
                    return;
                }

                var ctx = chart.ctx;
                var chartArea = chart.chartArea;
                if (!ctx || !chartArea) {
                    return;
                }

                ctx.save();
                ctx.font = font;
                ctx.textBaseline = "middle";

                chart.data.datasets.forEach(function (dataset, datasetIndex) {
                    var meta = chart.getDatasetMeta(datasetIndex);
                    if (!meta || meta.hidden) {
                        return;
                    }

                    meta.data.forEach(function (bar, dataIndex) {
                        var rawValue = dataset.data[dataIndex];
                        var value = Number(rawValue);
                        if (!Number.isFinite(value) || value <= minValue) {
                            return;
                        }

                        var barStartX = Math.min(bar.base, bar.x);
                        var barEndX = Math.max(bar.base, bar.x);
                        var barWidth = Math.max(0, barEndX - barStartX);
                        var barHeight = Math.max(0, bar.height || 0);
                        if (barWidth <= 0 || barHeight <= 0) {
                            return;
                        }

                        var labelCandidates = [
                            formatPercent(value, 2),
                            formatPercent(value, 1),
                            formatPercent(value, 0)
                        ];
                        var label = labelCandidates[0];
                        var maxLabelWidth = Math.max(0, barWidth - (offset * 2));
                        for (var i = 0; i < labelCandidates.length; i++) {
                            if (ctx.measureText(labelCandidates[i]).width <= maxLabelWidth) {
                                label = labelCandidates[i];
                                break;
                            }
                        }

                        var y = bar.y;
                        var labelWidth = ctx.measureText(label).width;
                        var fitsInside = labelWidth <= maxLabelWidth;

                        if (fitsInside) {
                            var insideX = barEndX - offset;

                            ctx.save();
                            ctx.beginPath();
                            ctx.rect(
                                barStartX + 1,
                                y - (barHeight / 2) + 1,
                                Math.max(0, barWidth - 2),
                                Math.max(0, barHeight - 2)
                            );
                            ctx.clip();

                            ctx.textAlign = "right";
                            ctx.fillStyle = insideTextColor;
                            ctx.fillText(label, insideX, y);
                            ctx.restore();
                            return;
                        }

                        // If text does not fit inside the bar, render it outside.
                        var outsideLabel = labelCandidates[0];
                        var outsideLabelWidth = ctx.measureText(outsideLabel).width;
                        var rightAnchorX = barEndX + outsideOffset;
                        var rightLimitX = chartArea.right - 2;
                        var availableRightWidth = Math.max(0, rightLimitX - rightAnchorX);
                        if (outsideLabelWidth > availableRightWidth) {
                            for (var j = 1; j < labelCandidates.length; j++) {
                                var candidateWidth = ctx.measureText(labelCandidates[j]).width;
                                if (candidateWidth <= availableRightWidth) {
                                    outsideLabel = labelCandidates[j];
                                    outsideLabelWidth = candidateWidth;
                                    break;
                                }
                            }
                        }

                        if (outsideLabelWidth <= availableRightWidth) {
                            ctx.textAlign = "left";
                            ctx.fillStyle = textColor;
                            ctx.fillText(outsideLabel, rightAnchorX, y);
                            return;
                        }

                        var leftAnchorX = barStartX - outsideOffset;
                        var leftLimitX = chartArea.left + 2;
                        if (leftAnchorX < leftLimitX) {
                            leftAnchorX = leftLimitX;
                        }

                        ctx.textAlign = "right";
                        ctx.fillStyle = textColor;
                        ctx.fillText(outsideLabel, leftAnchorX, y);
                    });
                });

                ctx.restore();
            }
        };
    };
})(window);
