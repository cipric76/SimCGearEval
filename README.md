# SimCGearEval
Enumerate gear sets and call SimulationCraft to simulate each set

# Known issues
On use items are not yet supported properly.
Enumerating talents is not yet implemented.

# Usage
The tool supports multiple profiles and that can be used to work around the lack of support for multiple talents
A configuration file must include(please see SimcGearTest for a sample):
 - The list of profiles to simulate. Each profile must be backed by a .simc file in the working folder
 - The path to simulation craft binaries
 - The working folder
 - A list of legendary items (In the future this gets hopefully built in)
Each .simc file must include the full profile as imported by SimulationCraft. There are some manual edits required as all fingerx= and trinketx= gear lines must be changed to finger= and trinket=. The first item from each slot is considered the equiped item. The tool will output the best DPS for each item included, the equiped set and its corresponding DPS, the best 10 DPS sets and the best set for each item in the simulation if the respective set was not included in the top 10 sets.
