import plotly.express as px


class Metrics:
    def __init__(self, metrics_json):
        self.metrics_json = metrics_json
        self.metrics = {metric["name"]: metric["value"] for metric in metrics_json}

    def has(self, metricName):
        if metricName not in self.metrics:
            print(f"No {metricName} metric.")
            return False
        else:
            return True

    def plot_simple_histogram(self, metric_name, x="value", y="count", **kwargs):
        if not self.has(metric_name):
            return

        fig = px.bar(self.metrics[metric_name], x=x, y=y, **kwargs)
        fig.show()

    def plot_numeric_histogram(self, **kwargs):
        METRIC_NAME = "histogram.buckets"
        self.plot_simple_histogram(METRIC_NAME, x="lowerBound", **kwargs)

    def plot_distinct_values(self, **kwargs):
        METRIC_NAME = "distinct.values"
        if not self.has(METRIC_NAME):
            METRIC_NAME = "distinct.top_values"
            if not self.has(METRIC_NAME):
                return

        fig = px.pie(self.metrics[METRIC_NAME], names="value", values="count", **kwargs)
        fig.show()

    def plot_cyclical_datetimes(
        self, time_component, theta="value", r="count", **kwargs
    ):
        METRIC_NAME = "dates_cyclical." + time_component

        if not self.has(METRIC_NAME):
            return

        scale = {
            "hour": 24,
            "minute": 60,
            "second": 60,
            "day": 31,
            "weekday": 7,
            "quarter": 4,
            "month": 12,
        }[time_component]

        plotData = [
            {
                r: metric[r],
                theta: metric[theta],
                "theta_scaled": metric[theta] * 360 / scale,
            }
            for metric in self.metrics[METRIC_NAME]["counts"]
        ]

        fig = px.bar_polar(
            plotData, r=r, theta="theta_scaled", hover_data=[theta], **kwargs
        )
        fig.show()

    def plot_linear_datetimes(self, time_component, x="value", y="count", **kwargs):
        METRIC_NAME = "dates_linear." + time_component

        if not self.has(METRIC_NAME):
            return

        fig = px.bar(self.metrics[METRIC_NAME]["counts"], x=x, y=y, **kwargs)
        fig.show()
