function explorerParams(dataSource, table, columns, apiURL, apiKey) {
    return {
        "DataSource": dataSource,
        "Table": table,
        "Columns": columns.constructor.name === "String" ? columns.split(",").map(s => s.trim()) : columns,
        "ApiUrl": apiURL || AIRCLOAK_API_URL,
        "ApiKey": apiKey || AIRCLOAK_API_KEY
    };
}

async function explorerCancel(baseURL, state) {
    if (state.exploration_id) {
        console.log("cancelling exploration:", state.exploration_id);
        const exploration_id = state.exploration_id;
        state.exploration_id = null;
        await _explorerGet(`${baseURL}/cancel/${exploration_id}`);
        console.log("cancelled exploration:", exploration_id);
    }
}

async function explorerRun(params, baseURL, state, fnShowResult) {
    const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms));
    params = params.constructor.name === "Array" ? explorerParams(params[0], params[1], params[2], params[3], params[4]) : params;
    baseURL = baseURL || EXPLORER_BASE_URL;
    state = state || {};
    let response = await _explorerPost(`${baseURL}/explore`, params);
    let delay = 32;
    let result_old = null;
    state.exploration_id = response.id;
    while (true) {
        let result_text = JSON.stringify(response, undefined, 2);
        if (result_text !== result_old) {
            console.log(response);
            result_old = result_text;
            fnShowResult && fnShowResult(result_text);
        }

        await sleep(delay);
        delay = Math.min(2 * delay, 1000);

        if (response.status === "Complete" || response.Status === "Canceled" || response.Status === "Error") {
            state.exploration_id = null;
            return response;
        }
        if (state.exploration_id === null) {
            console.log("stopping exploration:", response.id, "(exploration was cancelled)");
            return null;
        }

        response = await _explorerGet(`${baseURL}/result/${state.exploration_id}`);
    }
}

async function _explorerGet(endpoint) {
    return await _explorerFetch('GET', endpoint, null);
}

async function _explorerPost(endpoint, data) {
    return await _explorerFetch('POST', endpoint, data);
}

async function _explorerFetch(method, endpoint, data) {
    let options = {
        method: method,
        mode: 'cors',
        cache: 'no-cache',
    }

    if (data) {
        options.headers = { 'Content-Type': 'application/json' };
        options.body = JSON.stringify(data);
    }

    let result = null;
    await fetch(endpoint, options)
        .then(async response => {
            if (!response.ok) {
                let details = await response.text();
                console.error(response);
                console.error(details);
                throw `${response.statusText}\n${details}`;
            }
            return response;
        })
        .then(async response => result = await response.json())

    return result;
}
