import logging
import azure.functions as func
import json
import requests


def main(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')

    name = req.params.get('name')
    if not name:
        try:
            req_body = req.get_json()
        except ValueError:
            pass
        else:
            name = req_body.get('name')

    if name:
        return func.HttpResponse(f"Hello, {name}. This HTTP triggered function executed successfully.")
    else:
        get_address_history("test")
        return func.HttpResponse(
             "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response.",
             status_code=200
        )

def sortByHeight(value):
    return value["height"]

def get_address_history(address):
    request_url = "https://api.whatsonchain.com/v1/bsv/main/address/1MukqQe8TbYDhv9KvLNDyxcFpfmkBFMqMM/history"
    r = requests.get(request_url)
    data = json.loads(r.text)
    sortedData = sorted(data, key=sortByHeight, reverse=True)
    pubkey = data['pubkey']