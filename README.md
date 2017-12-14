# LCG Extractor
A project for OCTGN which integrates with online LCG card databases to generate OCTGN set xmls, and an attached deck editor plugin to allow users to easily install card images.  It's currently designed to be used for LCG games which have a json-based API database (such as www.thronesdb.com).

There are two sections to this project:

### ImageFetcherPlugin
An OCTGN deck editor plugin (dll) which loads a simple UI, allowing users to install missing card images.  This will work with any installed OCTGN game which includes Extractor config files in its installation.  See https://github.com/brine/AGoTv2-OCTGN/tree/master/GameDatabase/30c200c9-6c98-49a4-a293-106c06295c05/Extractor for an example on how to set up the Extractor folder in the game definition.

### DBExtractor
An EXE which outputs OCTGN set xml files for LCG games.  It uses the Extractor configuration files mentioned above to template the game's card properties.  This is meant for game developers only, and should not be distributed to players in the game definition package.
