# XIVSplits - FFXIV Split Timer
*Time your dungeon dashes faster than a sprinting chocobo*

**XIVSplits** is a split timer for Final Fantasy XIV speedruns. 
It is designed to be used with the [LiveSplit](http://livesplit.org/) timer program (or independently), 
and can be used to automatically split your timer when you complete certain quests or duties.

This is an early testing version, so please report any bugs you find.

## Installation
Under Dalamud settings -> Experimental -> Custom Plugin Repositories, add the following URL:
`https://raw.githubusercontent.com/PassiveModding/XIVSplits/main/repo.json`

## Usage
### Creating a Split Template
1. Open the plugin settings and go to the Splits tab.
2. Click the "Add Template" button.
3. Set a name for the template.		
4. Add splits to the template by clicking the "Add Split" button.
5. Set a name for the split and optionally set a best time for the split.
6. Set the template as active by choosing it from the dropdown

### Setting up triggers (objectives)
Objectives are the things that will trigger a split. Currently supported is the ability to trigger when a specific message is sent to chat or when an objective in a duty is completed.

- Generic Objectives are triggered at any time.
- Duty Objectives are only triggered when you are in a duty which matches the duty name.
- Chat Goals are only triggered when the specified message is sent to chat.
- Duty Goals are only triggered when the specified message is in the duty information widget and has it's progress set to 100%.

Auto Start and Auto Completion Split are preset objectives that will automatically start the timer when you enter a duty and automatically split when you complete it. (English only)
Objectives are matched using regular expressions, so you can use wildcards to match differne things.
For example, the auto start objective uses the following regex to match when the barrier is removed at the start of a duty and the timer begins:
`^(.+) has begun\.$`

You can use the Add Duty text box to search for a duty, it will pre-fill with some likely objectives based on data pulled using Lumina. A lot of objectives are not useable however due to them being boss popup text rather than a duty objective.


### LiveSplit Integration
1. Download and install [LiveSplit](http://livesplit.org/).
2. Make sure you have [LiveSplit server](https://github.com/LiveSplit/LiveSplit.Server) installed. 
3. Enable LiveSplit server in LiveSplit's settings. In LiveSplit, go to Edit Layout -> Add -> Control -> LiveSplit Server.
5. Start the LiveSplit server. In LiveSplit, go to Control -> Start Server.
6. Connect to the LiveSplit server from XIVSplits. In XIVSplits, go to LiveSplit -> Enter your LiveSplit server's IP address and port. (Default should be localhost and port 16834) -> Connect.
