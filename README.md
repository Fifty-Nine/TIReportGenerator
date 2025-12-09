# TIReportGenerator

A work-in-progress BepInEx plugin for *Terra Invicta* that generates comprehensive text-based strategic reports from your save games.

> **⚠️ WARNING: WORK IN PROGRESS**
> 
> This mod is in early development. Features are incomplete, fit-and-finish is lacking, and **crashes can be expected**. Use at your own risk and always backup your saves.

## Features

Currently, the mod generates reports on game load, dumping them to text files for easy external viewing. The structure of
the reports leaves a bit to be desired (e.g., omitting uninteresting "0" fields) but should convey all relevant information.

### Implemented Reports
*   **Faction Status:** Overview of faction resource stockpiles and incomes as well as CP and MC usage and capacities.
*   **Nations:** Summary of important national statistics per nation as well as control point details and relations.
*   **Fleets:** Detailed breakdown of all fleets, including ship composition, combat power, Delta-V, and acceleration.
*   **Habs:** Listing of all space habitats and stations.
*   **Ship Templates:** Technical specifications for faction ship designs, including resource costs and performance metrics.
*   **Technology:** Comprehensive status of the global tech tree and faction-specific engineering projects
*   **Prospecting:** Survey of all habitat sites in the solar system, respecting fog-of-war.

### Planned Reports
*   **Councilors:** Detailed information on councilor stats and equipped orgs.
*   **Armies:** Global list of Earth-based armies.
*   **Threat Assessment:** Alien activity report (Estimated hate levels, xenoforming, detected operations) respecting fog-of-war.

## Usage

1.  Build the plugin using `dotnet build`.
2.  Place `TIReportGenerator.dll` and `BetterConsoleTables.dll` into your `BepInEx/plugins` folder.
3.  Load a save game. The reports will be generated automatically upon successful load.
4.  Reports will be generated into a new directory named `report_[your save name]/` in your TI save folder.

In the future, I hope to implement Gitlab actions-based binary releases as well as make report generation more configurable (e.g.
allow generation on demand or at save time rather than load time).

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE.md) file for details.
