import time
import requests
import constants

from metrics import Metrics


class Explorer:
    # Cache Json Responses
    JSON_CACHE = {}

    # Force cache refresh
    JSON_CACHE_REFRESH_ALL = False

    def __init__(self, dataset, table, column):
        self.dataset = dataset
        self.table = table
        self.column = column
        self.cache_id = (dataset, table, column)

    def launch(self):
        r = requests.post(
            f"{constants.EXPLORER_URL}/explore",
            json={
                "ApiKey": constants.API_KEY,
                "DataSourceName": self.dataset,
                "TableName": self.table,
                "ColumnName": self.column,
            },
        )
        try:
            r.raise_for_status()
        except HttpError as e:
            print(f"Http Error {r.status_code}: {r.json()}")

        return r.json()

    def wait_result(self, id):
        poll_count = 0
        print(f"Polling results for {self.dataset}/{self.table}/{self.column}")
        while True:
            poll_count += 1
            print(f"polling...{poll_count}")

            r = requests.get(f"{constants.EXPLORER_URL}/result/{id}")
            r.raise_for_status()

            status = r.json()["status"]
            if status in ["Complete", "Error"]:
                print(f'Done, status "{status}"')
                return r.json()
            else:
                print(f'status is "{status}"')

            time.sleep(4)

    def explore(self, refresh_cache=False):
        if (
            refresh_cache
            or Explorer.JSON_CACHE_REFRESH_ALL
            or self.cache_id not in Explorer.JSON_CACHE
        ):

            launch_response = self.launch()

            if "id" not in launch_response:
                print(f"An error occured launching the query:\n{launch_response}")
                return {}

            result_json = self.wait_result(launch_response["id"])
            if result_json["status"] == "Error":
                desc = result_json["description"]
                print(f"Aircloak query {result_json['status']}: {desc}")
                return {}

            Explorer.JSON_CACHE[self.cache_id] = result_json
        else:
            print("Pulling response json from cache.")

        return Metrics(Explorer.JSON_CACHE[self.cache_id]["metrics"])

    def dump_json_cache(self):
        print(Explorer.JSON_CACHE[self.cache_id])
