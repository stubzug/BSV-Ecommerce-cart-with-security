import logging
import azure.functions as func
import requests
import json
import datetime
import maya
import pytz
from azure.cosmosdb.table.tableservice import TableService
from azure.cosmosdb.table.models import Entity
from . import paymail

utc=pytz.UTC

def main(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')

    try:
        req_body = req.get_json()
    except ValueError:
        pass
    else:
        payment_to_address = req_body.get('PaymentToAddress')
        required_payment_amount = float(req_body.get('RequiredPaymentAmount'))

        to_address_info = get_address_info(payment_to_address)
        payment_window = datetime.datetime.utcnow()
        payment_window = payment_window + datetime.timedelta(days=-50)
        exchange_rate = get_exchange_rate()
        conversion_threshold = .80

        payment_valid = process_transactions(payment_window, exchange_rate, conversion_threshold, required_payment_amount, to_address_info)
        
        return func.HttpResponse(body=str(payment_valid),
                                 status_code=200)
        

def black_listed(payment_from_address):
    table_service = TableService(account_name='rgedxbsvdiag', account_key='4WxXkmivBxExilliciKKZt6wHnFuZJuAnabkSP3Mw42WZfJBG9JMN9cCO07o7y4FNXLZ0dXPFpaYVkbudf0brQ==')
    try:
        table_service.get_entity('AddressBlackList', "1Gjf5yd2R7dYG4xWB5uvmPfX8raRV4oWbP", payment_from_address)
    except:
        return False

    return True

def validate_inputs(transaction):
    for payment_input in transaction['vin']:
        source_txid = payment_input['txid']
        source_trans = get_transaction(source_txid)
        for payment_output in source_trans['vout']:
            for address in payment_output['scriptPubKey']['addresses']:
                if black_listed(address):
                    return False
    return True

def process_transactions(payment_window, exchange_rate, conversion_threshold, required_payment_amount, to_address_info):
    address_history = get_address_history(to_address_info['address'])

    for history in address_history:
        #query transaction status using mapi api
        transaction_status = query_transaction_status(history['tx_hash'])
        if transaction_status != "success":
            continue
        
        transaction = get_transaction(history['tx_hash'])

        if not validate_inputs(transaction):
            return false
        
        #payment_date = get_payment_date(transaction)
        #if payment_date >= utc.localize(payment_window):
        for payment_output in transaction['vout']:
            payment_amount = float(payment_output['value']) * exchange_rate
            payment_percentage = round(payment_amount / required_payment_amount,2)
            if payment_percentage >= conversion_threshold and payment_percentage <= 1.10:
                payment_output_scriptpubkey = payment_output['scriptPubKey']['hex']
                to_address_scriptpubkey = to_address_info['scriptPubKey']
                if payment_output_scriptpubkey == to_address_scriptpubkey :
                    return True
                else:
                    for address in payment_output['scriptPubKey']['addresses']:
                        if address == to_address_info['address']:
                            return True
       
    return False

def query_transaction_status(tx_hash):
    request_url = "https://mapi.taal.com/mapi/tx/{}".format(tx_hash) 
    r = requests.get(request_url)
    data = json.loads(r.text)
    payload = json.loads(data['payload'])
    return_result = payload['returnResult']
    return return_result

def sort_by_height(value):
    return value["height"]        

def get_address_history(address):
    request_url = "https://api.whatsonchain.com/v1/bsv/main/address/" + address + "/history"
    r = requests.get(request_url)
    data = json.loads(r.text)
    sortedData = sorted(data, key=sort_by_height, reverse=True)
    return sortedData

def get_exchange_rate():
    request_url = "https://api.whatsonchain.com/v1/bsv/main/exchangerate"
    r = requests.get(request_url)
    data = json.loads(r.text)
    rate = float(data['rate'])
    return rate

def get_transaction(tx_hash):
    request_url = "https://api.whatsonchain.com/v1/bsv/main/tx/hash/" + tx_hash
    r = requests.get(request_url)
    data = json.loads(r.text)
    return data

def get_payment_date(transaction):
    unix_date = transaction['blocktime']
    add_seconds = maya.parse("1970-01-01T00:00:00Z").datetime()
    payment_date = add_seconds + datetime.timedelta(0,unix_date)
    return payment_date

def get_address_info(address):
    request_url = "https://api.whatsonchain.com/v1/bsv/main/address/{}/info".format(address) 
    r = requests.get(request_url)
    data = json.loads(r.text)
    return data



    
