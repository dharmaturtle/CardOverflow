module AnkiImportTestData

open CardOverflow.Api
open CardOverflow.Entity.Anki
open CardOverflow.Test
open CardOverflow.Pure
open CardOverflow.Debug
open Microsoft.EntityFrameworkCore
open Microsoft.FSharp.Quotations
open Helpers
open System.IO
open System.IO.Compression
open System.Linq
open Xunit
open CardOverflow.Sanitation

let allDefaultTemplatesAndImageAndMp3_apkg =
    {
        Cards = [
            CardEntity(
                Id = 1554689693560L,
                Nid = 1554689672054L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 1L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689744008L,
                Nid = 1554689697908L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 2L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689744009L,
                Nid = 1554689697908L,
                Did = 1L,
                Ord = 1L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 2L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689782082L,
                Nid = 1554689750007L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 3L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689782083L,
                Nid = 1554689750007L,
                Did = 1L,
                Ord = 1L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 3L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689799193L,
                Nid = 1554689787164L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 4L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689885405L,
                Nid = 1554689802142L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 5L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554690071566L,
                Nid = 1554689900910L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 6L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554690471678L,
                Nid = 1554690071622L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 7L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1560646410586L,
                Nid = 1560646400431L,
                Did = 1L,
                Ord = 0L,
                Mod = 1560646410L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 8L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            ]
        Cols = [
            ColEntity(
                Id = 1L,
                Crt = 1556096400L,
                Mod = 1560646804961L,
                Scm = 1560646804942L,
                Ver = 11L,
                Dty = 0L,
                Usn = 0L,
                Ls = 0L,
                Conf = "{\"activeDecks\": [1], \"curDeck\": 1, \"newSpread\": 0, \"collapseTime\": 1200, \"timeLim\": 0, \"estTimes\": true, \"dueCounts\": true, \"curModel\": \"1560646804944\", \"nextPos\": 1, \"sortType\": \"noteFld\", \"sortBackwards\": false, \"addToCur\": true, \"dayLearnFirst\": false, \"newBury\": true}",
                Models = "{\"1554689669581\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646410, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [], \"id\": 1554689669581, \"req\": [[0, \"all\", [0]]]}, \"1554689669577\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646288, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (and reversed card)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}, {\"name\": \"Card 2\", \"ord\": 1, \"qfmt\": \"{{Back}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Front}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669577, \"req\": [[0, \"all\", [0]], [1, \"all\", [1]]]}, \"1554689669572\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646292, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (optional reversed card)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Add Reverse\", \"ord\": 2, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}, {\"name\": \"Card 2\", \"ord\": 1, \"qfmt\": \"{{#Add Reverse}}{{Back}}{{/Add Reverse}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Front}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669572, \"req\": [[0, \"all\", [0]], [1, \"all\", [1, 2]]]}, \"1554689669571\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646306, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (type in the answer)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\\n{{type:Back}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669571, \"req\": [[0, \"all\", [0]]]}, \"1554689669570\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646315, \"usn\": -1, \"vers\": [], \"type\": 1, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\\n.cloze {\\n font-weight: bold;\\n color: blue;\\n}\\n.nightMode .cloze {\\n color: lightblue;\\n}\", \"name\": \"Cloze\", \"flds\": [{\"name\": \"Text\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Extra\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Cloze\", \"ord\": 0, \"qfmt\": \"{{cloze:Text}}\", \"afmt\": \"{{cloze:Text}}<br>\\n{{Extra}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669570}}",
                Decks = "{\"1\": {\"newToday\": [0, 0], \"revToday\": [0, 0], \"lrnToday\": [0, 0], \"timeToday\": [0, 0], \"conf\": 1, \"usn\": 0, \"desc\": \"\", \"dyn\": 0, \"collapsed\": false, \"extendNew\": 10, \"extendRev\": 50, \"id\": 1, \"name\": \"Default\", \"mod\": 1560646804}}",
                Dconf = "{\"1\": {\"name\": \"Default\", \"new\": {\"delays\": [1, 10], \"ints\": [1, 4, 7], \"initialFactor\": 2500, \"separate\": true, \"order\": 1, \"perDay\": 20, \"bury\": false}, \"lapse\": {\"delays\": [10], \"mult\": 0, \"minInt\": 1, \"leechFails\": 8, \"leechAction\": 0}, \"rev\": {\"perDay\": 200, \"ease4\": 1.3, \"fuzz\": 0.05, \"minSpace\": 1, \"ivlFct\": 1, \"maxIvl\": 36500, \"bury\": false, \"hardFactor\": 1.2}, \"maxTaken\": 60, \"timer\": 0, \"autoplay\": true, \"replayq\": true, \"mod\": 0, \"usn\": 0, \"id\": 1}}",
                Tags = "{}")
            ]
        Notes = [
            NoteEntity(
                Id = 1554689672054L,
                Guid = "hdW&ZLKq1B",
                Mid = 1554689669581L,
                Mod = 1554689693L,
                Usn = -1L,
                Tags = " Basic Tag ",
                Flds = "Basic FrontBasic Back",
                Sfld = 0L,
                Csum = 3392947020L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689697908L,
                Guid = "Dq.!@mFght",
                Mid = 1554689669577L,
                Mod = 1554689744L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic (and reversed card) frontBasic (and reversed card) back",
                Sfld = 0L,
                Csum = 1851683185L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689750007L,
                Guid = "q3-~k#2MqI",
                Mid = 1554689669572L,
                Mod = 1554689782L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic (optional reversed card) frontBasic (optional reversed card) backBasic (optional reversed card) reverse",
                Sfld = 0L,
                Csum = 1310466425L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689787164L,
                Guid = "w+4I$EJ/NQ",
                Mid = 1554689669571L,
                Mod = 1554689799L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic (type in the answer) frontBasic (type in the answer) back",
                Sfld = 0L,
                Csum = 4281307022L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689802142L,
                Guid = "yuqNm3;_r2",
                Mid = 1554689669570L,
                Mod = 1554689885L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Cloze text.&nbsp;Canberra was founded in {{c1::1913}}.Cloze extra",
                Sfld = 0L,
                Csum = 1283850154L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689900910L,
                Guid = "f_TwXvEN<g",
                Mid = 1554689669581L,
                Mod = 1554691431L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic with image&nbsp;<img src=\"favicon.ico\">Basic back, no image",
                Sfld = 0L,
                Csum = 2183677625L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554690071622L,
                Guid = "xa]y)`6T[0",
                Mid = 1554689669581L,
                Mod = 1554691132L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic front with mp3[sound:bloop.wav]Basic back, no mp3",
                Sfld = 0L,
                Csum = 283440196L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1560646400431L,
                Guid = "s_4H3`+|ZV",
                Mid = 1554689669581L,
                Mod = 1560646587L,
                Usn = -1L,
                Tags = "",
                Flds = "<img src=\"png1.png\"><img src=\"png2.png\">",
                Sfld = 0L,
                Csum = 364157591L,
                Flags = 0L,
                Data = "")
            ]
        Revlogs = []
    }

let allDefaultTemplatesAndImageAndMp3_colpkg =
    {
        Cards = [
            CardEntity(
                Id = 1554689693560L,
                Nid = 1554689672054L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 1L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689744008L,
                Nid = 1554689697908L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 2L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689744009L,
                Nid = 1554689697908L,
                Did = 1L,
                Ord = 1L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 2L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689782082L,
                Nid = 1554689750007L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 3L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689782083L,
                Nid = 1554689750007L,
                Did = 1L,
                Ord = 1L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 3L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689799193L,
                Nid = 1554689787164L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 4L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554689885405L,
                Nid = 1554689802142L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 5L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554690071566L,
                Nid = 1554689900910L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 6L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1554690471678L,
                Nid = 1554690071622L,
                Did = 1L,
                Ord = 0L,
                Mod = 1556154677L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 7L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            CardEntity(
                Id = 1560646410586L,
                Nid = 1560646400431L,
                Did = 1L,
                Ord = 0L,
                Mod = 1560646410L,
                Usn = -1L,
                Type = 0L,
                Queue = 0L,
                Due = 8L,
                Ivl = 0L,
                Factor = 0L,
                Reps = 0L,
                Lapses = 0L,
                Left = 0L,
                Odue = 0L,
                Odid = 0L,
                Flags = 0L,
                Data = "")
            ]
        Cols = [
            ColEntity(
                Id = 1L,
                Crt = 1556096400L,
                Mod = 1560646700734L,
                Scm = 1560646279929L,
                Ver = 11L,
                Dty = 0L,
                Usn = 0L,
                Ls = 0L,
                Conf = "{\"activeDecks\": [1], \"curDeck\": 1, \"newSpread\": 0, \"collapseTime\": 1200, \"timeLim\": 0, \"estTimes\": true, \"dueCounts\": true, \"curModel\": 1554689669581, \"nextPos\": 9, \"sortType\": \"noteFld\", \"sortBackwards\": false, \"addToCur\": true, \"dayLearnFirst\": false, \"newBury\": true, \"activeCols\": [\"noteFld\", \"template\", \"cardDue\", \"deck\"], \"nightMode\": false, \"schedVer\": 2, \"rollover\": 4, \"lastUnburied\": 52, \"previewBothSides\": true}",
                Models = "{\"1554689669581\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646410, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [], \"id\": 1554689669581, \"req\": [[0, \"all\", [0]]]}, \"1554689669577\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646288, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (and reversed card)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}, {\"name\": \"Card 2\", \"ord\": 1, \"qfmt\": \"{{Back}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Front}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669577, \"req\": [[0, \"all\", [0]], [1, \"all\", [1]]]}, \"1554689669572\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646292, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (optional reversed card)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Add Reverse\", \"ord\": 2, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}, {\"name\": \"Card 2\", \"ord\": 1, \"qfmt\": \"{{#Add Reverse}}{{Back}}{{/Add Reverse}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Front}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669572, \"req\": [[0, \"all\", [0]], [1, \"all\", [1, 2]]]}, \"1554689669571\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646306, \"usn\": -1, \"vers\": [], \"type\": 0, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\", \"name\": \"Basic (type in the answer)\", \"flds\": [{\"name\": \"Front\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Back\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Card 1\", \"ord\": 0, \"qfmt\": \"{{Front}}\\n{{type:Back}}\", \"afmt\": \"{{FrontSide}}\\n\\n<hr id=answer>\\n\\n{{Back}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669571, \"req\": [[0, \"all\", [0]]]}, \"1554689669570\": {\"sortf\": 0, \"did\": 1, \"latexPre\": \"\\\\documentclass[12pt]{article}\\n\\\\special{papersize=3in,5in}\\n\\\\usepackage[utf8]{inputenc}\\n\\\\usepackage{amssymb,amsmath}\\n\\\\pagestyle{empty}\\n\\\\setlength{\\\\parindent}{0in}\\n\\\\begin{document}\\n\", \"latexPost\": \"\\\\end{document}\", \"mod\": 1560646315, \"usn\": -1, \"vers\": [], \"type\": 1, \"css\": \".card {\\n font-family: arial;\\n font-size: 20px;\\n text-align: center;\\n color: black;\\n background-color: white;\\n}\\n\\n.cloze {\\n font-weight: bold;\\n color: blue;\\n}\\n.nightMode .cloze {\\n color: lightblue;\\n}\", \"name\": \"Cloze\", \"flds\": [{\"name\": \"Text\", \"ord\": 0, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}, {\"name\": \"Extra\", \"ord\": 1, \"sticky\": false, \"rtl\": false, \"font\": \"Arial\", \"size\": 20, \"media\": []}], \"tmpls\": [{\"name\": \"Cloze\", \"ord\": 0, \"qfmt\": \"{{cloze:Text}}\", \"afmt\": \"{{cloze:Text}}<br>\\n{{Extra}}\", \"did\": null, \"bqfmt\": \"\", \"bafmt\": \"\"}], \"tags\": [\"OtherTag\"], \"id\": 1554689669570}}",
                Decks = "{\"1\": {\"newToday\": [0, 0], \"revToday\": [0, 0], \"lrnToday\": [0, 0], \"timeToday\": [0, 0], \"conf\": 1, \"usn\": -1, \"desc\": \"\", \"dyn\": 0, \"collapsed\": false, \"extendNew\": 10, \"extendRev\": 50, \"id\": 1, \"name\": \"Default\", \"mod\": 1556154677}}",
                Dconf = "{\"1\": {\"name\": \"Default\", \"new\": {\"delays\": [1, 10], \"ints\": [1, 4, 7], \"initialFactor\": 2500, \"separate\": true, \"order\": 1, \"perDay\": 20, \"bury\": false}, \"lapse\": {\"delays\": [10], \"mult\": 0, \"minInt\": 1, \"leechFails\": 8, \"leechAction\": 0}, \"rev\": {\"perDay\": 200, \"ease4\": 1.3, \"fuzz\": 0.05, \"minSpace\": 1, \"ivlFct\": 1, \"maxIvl\": 36500, \"bury\": false, \"hardFactor\": 1.2}, \"maxTaken\": 60, \"timer\": 0, \"autoplay\": true, \"replayq\": true, \"mod\": 0, \"usn\": 0, \"id\": 1, \"dyn\": false}}",
                Tags = "{\"Basic\": -1, \"OtherTag\": -1, \"Tag\": -1}")
            ]
        Notes = [
            NoteEntity(
                Id = 1554689672054L,
                Guid = "hdW&ZLKq1B",
                Mid = 1554689669581L,
                Mod = 1554689693L,
                Usn = -1L,
                Tags = " Basic Tag ",
                Flds = "Basic FrontBasic Back",
                Sfld = 0L,
                Csum = 3392947020L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689697908L,
                Guid = "Dq.!@mFght",
                Mid = 1554689669577L,
                Mod = 1554689744L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic (and reversed card) frontBasic (and reversed card) back",
                Sfld = 0L,
                Csum = 1851683185L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689750007L,
                Guid = "q3-~k#2MqI",
                Mid = 1554689669572L,
                Mod = 1554689782L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic (optional reversed card) frontBasic (optional reversed card) backBasic (optional reversed card) reverse",
                Sfld = 0L,
                Csum = 1310466425L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689787164L,
                Guid = "w+4I$EJ/NQ",
                Mid = 1554689669571L,
                Mod = 1554689799L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic (type in the answer) frontBasic (type in the answer) back",
                Sfld = 0L,
                Csum = 4281307022L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689802142L,
                Guid = "yuqNm3;_r2",
                Mid = 1554689669570L,
                Mod = 1554689885L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Cloze text.&nbsp;Canberra was founded in {{c1::1913}}.Cloze extra",
                Sfld = 0L,
                Csum = 1283850154L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554689900910L,
                Guid = "f_TwXvEN<g",
                Mid = 1554689669581L,
                Mod = 1554691431L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic with image&nbsp;<img src=\"favicon.ico\">Basic back, no image",
                Sfld = 0L,
                Csum = 2183677625L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1554690071622L,
                Guid = "xa]y)`6T[0",
                Mid = 1554689669581L,
                Mod = 1554691132L,
                Usn = -1L,
                Tags = " OtherTag ",
                Flds = "Basic front with mp3[sound:bloop.wav]Basic back, no mp3",
                Sfld = 0L,
                Csum = 283440196L,
                Flags = 0L,
                Data = "")
            NoteEntity(
                Id = 1560646400431L,
                Guid = "s_4H3`+|ZV",
                Mid = 1554689669581L,
                Mod = 1560646587L,
                Usn = -1L,
                Tags = "",
                Flds = "<img src=\"png1.png\"><img src=\"png2.png\">",
                Sfld = 0L,
                Csum = 364157591L,
                Flags = 0L,
                Data = "")
            ]
        Revlogs = []
    }

type All () = // https://stackoverflow.com/questions/35026735/
    let values : seq<obj[]>  =
        seq {
            yield [|allDefaultTemplatesAndImageAndMp3_apkg |]
            yield [|allDefaultTemplatesAndImageAndMp3_colpkg |]
        }
    interface seq<obj[]> with
        member __.GetEnumerator () = values.GetEnumerator()
        member __.GetEnumerator () =
            values.GetEnumerator() :> System.Collections.IEnumerator

let ankiExportsDir = Directory.GetCurrentDirectory() +/ "AnkiExports"

let unzipTest ankiFileName entry =
    let unzipDir = ankiExportsDir +/ "Temp" +/ ankiFileName // Need to isolate ankiDb otherwise tests run in parallel fail
    if Directory.Exists unzipDir
    then Directory.Delete(unzipDir, true)
    Directory.CreateDirectory unzipDir |> ignore
    Anki.unzipToRandom (ankiExportsDir +/ ankiFileName) entry unzipDir

let getAnki2 ankiFileName =
    unzipTest ankiFileName "collection.anki2" |> AnkiDbFactory.Create

let getAnki21 ankiFileName =
    unzipTest ankiFileName "collection.anki21" |> AnkiDbFactory.Create

let serialize x =
    ObjectDumper.Dump(x, DumpOptions(DumpStyle = DumpStyle.CSharp,
                                     LineBreakChar = "",
                                     IndentSize = 0))

[<Fact>]
let ``AnkiImportTestData.allDefaultTemplatesAndImageAndMp3_colpkg matches AllDefaultTemplatesAndImageAndMp3.colpkg``() =
    let actualDb = 
        AnkiImporter.getSimpleAnkiDb
        |> using(getAnki21 "AllDefaultTemplatesAndImageAndMp3-21.colpkg")
    
    let mock = allDefaultTemplatesAndImageAndMp3_colpkg

    serialize actualDb.Revlogs = serialize mock.Revlogs |> Assert.True
    serialize actualDb.Cols = serialize mock.Cols |> Assert.True
    serialize actualDb.Notes = serialize mock.Notes |> Assert.True
    serialize actualDb.Cards = serialize mock.Cards |> Assert.True

[<Fact>]
let ``AnkiImportTestData.allDefaultTemplatesAndImageAndMp3_apkg matches AllDefaultTemplatesAndImageAndMp3.apkg``() =
    let actualDb =
        AnkiImporter.getSimpleAnkiDb
        |> using(getAnki2 "AllDefaultTemplatesAndImageAndMp3.apkg")
    
    let mock = allDefaultTemplatesAndImageAndMp3_apkg

    serialize actualDb.Revlogs = serialize mock.Revlogs |> Assert.True
    serialize actualDb.Cols = serialize mock.Cols |> Assert.True
    serialize actualDb.Notes = serialize mock.Notes |> Assert.True
    serialize actualDb.Cards = serialize mock.Cards |> Assert.True

let emptyDb = {
        Cards = []
        Cols = allDefaultTemplatesAndImageAndMp3_colpkg.Cols
        Notes = []
        Revlogs = []
    }

[<Theory>]
[<InlineData("AllDefaultTemplatesAndImageAndMp3.apkg")>]
[<InlineData("AllDefaultTemplatesAndImageAndMp3.colpkg")>]
let ``AnkiImporter.save saves two files``(ankiFileName) =
    let userId = 3
    use c = new TestContainer(ankiFileName)
    
    ankiExportsDir +/ ankiFileName
    |> AnkiImporter.loadFiles userId (c.Db.Files.Where(fun x -> x.UserId = userId))
    |> Result.bind (AnkiImporter.save c.Db emptyDb userId)
    |> Result.getOk

    Assert.Equal(4, c.Db.Files.Count()) // medTODO use hash to dedupe png.png; this value really should be 3

[<Theory>]
[<InlineData("AllDefaultTemplatesAndImageAndMp3.apkg")>]
[<InlineData("AllDefaultTemplatesAndImageAndMp3.colpkg")>]
[<InlineData("AllDefaultTemplatesAndImageAndMp3-21.colpkg")>]
let ``Running AnkiImporter.save 2x yields errors due to duplicate filenames``(ankiFileName) =
    let userId = 3
    use c = new TestContainer(ankiFileName)
    
    let actual =
        [1..2]
        |> List.map (fun _ ->
            ankiExportsDir +/ ankiFileName
            |> AnkiImporter.loadFiles userId (c.Db.Files.Where(fun x -> x.UserId = userId))
            |> Result.bind (AnkiImporter.save c.Db emptyDb userId)
        )

    let expected =
        [ Ok ()
          Error "You already have a file called 'bloop.wav'.
You already have a file called 'favicon.ico'.
You already have a file called 'png1.png'.
You already have a file called 'png2.png'." ]
    Assert.Equal<Result<unit, string> list>(expected, actual)  // medTODO use hash to dedupe
