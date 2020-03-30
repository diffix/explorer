function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}


async function sfetch(method, endpoint, data, fn_show_error) {
    let options = {
        method: method,
        mode: 'cors',
        cache: 'no-cache',
    };

    if (data) {
        options.headers = { 'Content-Type': 'application/json' };
        options.body = JSON.stringify(data);
    }

    let result = null;

    await fetch(`${EXPLORER_BASE_URL}/${endpoint}`, options)
        .then(async response => {
            if (!response.ok) {
                let details = await response.text();
                console.error(response);
                console.error(details);
                show_error(response.statusText, details);
                throw `${response.statusText}\n${details}`;
            }
            return response;
        })
        .then(async response => result = await response.json())
        .catch(fn_show_error);

    return result;
}

async function fetch_get(endpoint, fn_show_error) {
    return await sfetch('GET', endpoint, null, fn_show_error);
}

async function fetch_post(endpoint, data, fn_show_error) {
    return await sfetch('POST', endpoint, data, fn_show_error);
}

function show_error(error, details) {
    let div_result = document.getElementById("result");
    div_result.innerHTML = `<div class='w3-text-dark-red'>${error}<br />${details || ""}</div>`;
}

let query_id = null;

async function explore() {
    try {
        explore_impl();
    }
    finally {
        query_id = null;
    }
}

async function explore_impl() {
    let div_result = document.getElementById("result");
    let data_source = document.getElementById("txt_data_source").value;
    let table_name = document.getElementById("txt_table").value;
    let column_name = document.getElementById("txt_column").value;

    let data = {
        "DataSourceName": data_source,
        "TableName": table_name,
        "ColumnName": column_name,
        "ApiKey": AIRCLOAK_API_KEY
    };

    let response = await fetch_post('explore', data, show_error);
    let delay = 32;
    let result_old = null;
    query_id = response.id;
    while (true) {
        let result_text = JSON.stringify(response, undefined, 2);
        if (result_text != result_old) {
            console.log(response);
            result_old = result_text;
            div_result.innerText = result_text;
        }

        if (response.status == "Complete" || response.Status == "Canceled" || response.Status == "Error") {
            return;
        }
        if (query_id == null) {
            return;
        }

        response = await fetch_get(`result/${query_id}`, show_error);

        await sleep(delay);
        delay = Math.min(2 * delay, 1000);
    }
}

function cancel() {
    if (query_id) {
        console.log("cancel:", query_id);
        fetch_get(`cancel/${query_id}`, show_error);
        show_error('');
        query_id = null;
    }
}