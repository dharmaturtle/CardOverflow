const unsynced = "1970-01-01T00:00:13Z"; // see `unsynced` in Infrastructure.fs

function getDb() {
    let db = new Dexie("MainDatabase");
    db.version(1).stores({
        DeckStream: "commandId,streamId,clientCreatedAt,serverCreatedAt",
        DeckSummary: "id,name,description",
    });
    return db;
}

function bulkPutEvents(tablePrefix, eventsString, summaryString) {
    let parsedSummaries = JSON.parse(summaryString);
    let summaries = Object.keys(parsedSummaries).map(key => {
        return {
            id         : key,
            name       : parsedSummaries[key].Fields[0].Name,
            description: parsedSummaries[key].Fields[0].Description,
            summary    : JSON.stringify(parsedSummaries[key])
        };
    });
    let events = JSON.parse(eventsString).map(event => {
        return {
            commandId      : event.Event.Fields[0].Meta.CommandId,
            streamId       : event.StreamId,
            clientCreatedAt: event.Event.Fields[0].Meta.ClientCreatedAt,
            serverCreatedAt: event.Event.Fields[0].Meta.ServerCreatedAt?.Fields[0] ?? unsynced,
            event          : JSON.stringify(event)
        };
    });
    let db = getDb();
    let tableStream = tablePrefix + "Stream";
    let tableSummary = tablePrefix + "Summary";
    return db.transaction('rw', tableStream, tableSummary, async () => {
        await db.table(tableStream).bulkPut(events);
        await db.table(tableSummary).bulkPut(summaries);
    });
}

function getUnsyncedEvents(table) {
    return getDb()
        .table(table)
        .where('serverCreatedAt')
        .equals(unsynced)
        .toArray(row => { return row.map(e => e.event) }); // can't "select" just the event from indexeddb, so we `map` it https://github.com/dfahlander/Dexie.js/issues/468
};
