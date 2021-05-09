const unsynced = "1970-01-01T00:00:13Z"; // see `unsynced` in Infrastructure.fs

function getDb() {
    let db = new Dexie("MainDatabase");
    db.version(1).stores({
        DeckStream  : "commandId,streamId,clientCreatedAt,serverCreatedAt",
        StackStream : "commandId,streamId,clientCreatedAt,serverCreatedAt",
        DeckSummary : "id,name,description",
        StackSummary: "id",
    });
    return db;
}

function deckSummaries(summaries) {
    return Object.keys(summaries).map(key => {
        return {
            id         : key,
            name       : summaries[key].Fields[0].Name,
            description: summaries[key].Fields[0].Description,
            summary    : JSON.stringify(summaries[key])
        };
    });
}

function stackSummaries(summaries) {
    return Object.keys(summaries).map(key => {
        return {
            id     : key,
            summary: JSON.stringify(summaries[key])
        };
    });
}

function bulkPutEvents(tablePrefix, eventsString, summaryString) {
    let parsedSummaries = JSON.parse(summaryString);
    let summaries =
        tablePrefix === "Deck"  ? deckSummaries(parsedSummaries)  :
        tablePrefix === "Stack" ? stackSummaries(parsedSummaries) :
        (function () { throw "Unsupported table: " + tablePrefix }());
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

function getUnsynced(table) {
    return table
        .where('serverCreatedAt')
        .equals(unsynced)
        .toArray(row => { return row.map(e => e.event) }); // can't "select" just the event from indexeddb, so we `map` it https://github.com/dfahlander/Dexie.js/issues/468
}

function getAllUnsynced() {
    let db = getDb();
    return db.transaction('r', "DeckStream", "StackStream", async () => {
        return Promise.all([
            getUnsynced(db.table("DeckStream")),
            getUnsynced(db.table("StackStream"))
        ]);
    });
};
