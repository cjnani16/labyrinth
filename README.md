# Labyrinth

## About
An NPC AI framework that uses a linguistic modeling and context-free grammars to parse sentences, store knowledge, deduce information, and plan, all using human readable sentences. I plan for it to be used in a game similar to [Chris Chrawford's *Siboot*](https://en.wikipedia.org/wiki/Trust_%26_Betrayal:_The_Legacy_of_Siboot), but in a more interpretable language.

You can see it in action here:

[![Labybrinth demo video](https://img.youtube.com/vi/ElAYVwdFqyc/0.jpg)](https://www.youtube.com/watch?v=ElAYVwdFqyc&t=40s "Labyrinth Demo")

And you can hear my full explanation of the system in this 5-minute video!

[![Labybrinth explainer video](http://img.youtube.com/vi/YOUTUBE_VIDEO_ID_HERE/0.jpg)](http://www.youtube.com/watch?v=YOUTUBE_VIDEO_ID_HERE "Video Title")

## Features
- [x] Universal Grammar based parsing of sentences into the "Knowledge Graph" representation (see video)
- [x] First order logic for determining the entailments and truth status of parsed sentences
- [x] Parsing if-then sentences (implications) into "rules"
- [x] Planning system (using GOAP algorithm) that can create a plan to make a sentence true using a relevant subset of known "rules"
- [ ] Utility based goal/emotional state generating system, using Mazlow's hierarchy (https://github.com/cjnani16/labyrinth/issues/1)
- [ ] Salience ratings for knowledge in the knowledge graph to determine situational relevance of knowledge
- [ ] Social behavior system that performs certain conversational acts (Informing, asking questions/seeking information, requesting favors) (https://github.com/cjnani16/labyrinth/issues/5)
- [ ] Authoring content using the system!

## Project status
Paused ⏸️

Since starting grad school, I've paused work on this project to make some simpler games for my portfolio. But I'll be back to this eventually! It's a really exciting system. 
