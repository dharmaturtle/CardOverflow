const unsynced = "1970-01-01T00:00:13Z"; // see `unsynced` in Infrastructure.fs

function getDb() {
    let db = new Dexie("MainDatabase");
    db.version(1).stores({
        UserStream     : "commandId,streamId,clientCreatedAt,serverCreatedAt",
        DeckStream     : "commandId,streamId,clientCreatedAt,serverCreatedAt",
        TemplateStream : "commandId,streamId,clientCreatedAt,serverCreatedAt",
        ExampleStream  : "commandId,streamId,clientCreatedAt,serverCreatedAt",
        StackStream    : "commandId,streamId,clientCreatedAt,serverCreatedAt",
        
        UserSummary    : "id",
        DeckSummary    : "id,name,description",
        TemplateSummary: "id",
        ExampleSummary : "id",
        StackSummary   : "id,*dues",

        CardSummary    : "id,due,state,deckId",
    });
    return db;
}

function bulkPutEvents(tablePrefix, eventsString, summaries) {
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

function bulkPutSummaries(table, summaries) {
    return getDb().table(table).bulkPut(summaries);
}

function getUnsynced(table) {
    return table
        .where('serverCreatedAt')
        .equals(unsynced)
        .toArray(row => { return row.map(e => e.event) }); // can't "select" just the event from indexeddb, so we `map` it https://github.com/dfahlander/Dexie.js/issues/468
}

function getAllUnsynced() {
    let db = getDb();
    return db.transaction('r', "UserStream", "DeckStream", "TemplateStream", "ExampleStream", "StackStream", async () => {
        return Promise.all([
            getUnsynced(db.table("UserStream")),
            getUnsynced(db.table("DeckStream")),
            getUnsynced(db.table("TemplateStream")),
            getUnsynced(db.table("ExampleStream")),
            getUnsynced(db.table("StackStream"))
        ]);
    });
};

function tenMinutesFromNow() {
    return (new Date(Date.now() + (10 * 60 * 1000))).toISOString(); // https://stackoverflow.com/a/1197939
}

function getNextQuizCard() {
    return getDb()
        .StackSummary
        .where('dues')
        .below(tenMinutesFromNow())
        .first()
        //.orderBy('dues') // not needed; it's "naturally sorted by the index or primary key that was used in the where() clause" - https://dexie.org/docs/Collection/Collection.sortBy() see also https://github.com/dfahlander/Dexie.js/issues/297
        .then(x => { return x?.summary });
};

function getSummary(tablePrefix, id) {
    return getDb()
        .table(tablePrefix + "Summary")
        .where('id')
        .equals(id)
        .first()
        .then(x => { return x?.summary });
};

async function getViewDeck(db, deck) {
    let allCards = db.CardSummary.where("deckId").equals(deck.id);
    return {
        summary: deck.summary,
        allCount: await allCards.count(),
        dueCount: await allCards.and(card => { return card.due < tenMinutesFromNow(); }).count(),
    };
};

async function getViewDecks() {
    let db = getDb();
    return db.transaction('r', "DeckSummary", "CardSummary", async () => {
        let decks = await db.DeckSummary.toArray();
        return Promise.all(
            decks.map(d => { return getViewDeck(db, d); })
        );
    });
}
