GenTool for COMMAND AND CONQUER GENERALS: ZERO HOUR

Author: xezon

-----------------------------------------------------------------------------------------------
--- Install -----------------------------------------------------------------------------------

1. Copy the "d3d8.dll" file into the game install directory, for example:
   > C:\Program Files (x86)\EA Games\Command & Conquer Generals Zero Hour


-----------------------------------------------------------------------------------------------
--- Changelog ---------------------------------------------------------------------------------

New Features 8.6

    Added PNG screenshot option in GenTool Menu
    Added limited feature compatibility for Thyme

Fixes/Improvements 8.6

    Fixed crash on game start affecting some users

-----------------------------------------------------------------------------------------------

New Features 8.5

    Added auto detection for modifications that require full viewport (Control Bar Pro)
    Added fixes for scroll speed to act consistently in different resolutions, viewports, pitch

Fixes/Improvements 8.5

    Reworked GenTool functionality related to accessing BIG Files

-----------------------------------------------------------------------------------------------

New Features 8.4

    Added new Upload Mode for Replays and Screenshots

Fixes/Improvements 8.4

    Fixed potential MDS false positives with "OverlordGattlingCannon"
    Fixed broken version number in GenTool menu

-----------------------------------------------------------------------------------------------

Fixes/Improvements 8.3

    Fixed crash when pressing Shift + Number while playing
    Fixed crash on application boot with Windows XP 32 bit

-----------------------------------------------------------------------------------------------

New Features 8.2

    Added alternative brightness settings at +129 to +256

Fixes/Improvements 8.2

    Fixed issue with Money Display
    Fixed issue with text in MDS Popup
    Changed SAVE REP, SAVE ALL Upload Mode options to also store censored Replay file on disk

-----------------------------------------------------------------------------------------------

New Features 8.1

    Added new User ID generation for Upload Mode
    Added some user hardware information in uploaded *.txt file
    Added fix for game bug that would crash all clients in a multiplayer match
    Added fix for black supply piles in Replay playback
    Added full viewport support with command line argument "-forcefullviewport"

Fixes/Improvements 8.1

    Fixed wrong names in Money Display on some maps ("Rain", "Civilian Res")
    Changed GenTool brightness option range to -128 +128

-----------------------------------------------------------------------------------------------

New Features 8.0

    Added Replay Controls bar at bottom of the screen in Replay mode
    Added feature to set a Time Pause (JK) in Replay to Fast Forward (F) to
    Added Mod support for player colors in multiplayer.ini that will work with Money Display
    Added censorship of IP addresses for Replay and Text files uploaded with GenTool

Fixes/Improvements 8.0

    Fixed a crash on game launch with Zero Hour The First Decade installations
    Fixed an issue where GenTool notification popup would never disappear after game launch
    Fixed an issue where game controls would freeze at match start
    Fixed an issue where Generals would freeze on shutdown
    Fixed an issue where WindowZH.big and Window.big could no longer be patched
    Fixed an issue that would log file names incorrectly in gentool.log file
    Fixed an issue that would generate images for upload after game session was completed
    Fixed an issue that would break Upload Mode when toggling its menu option
    Removed LastReplay.rep functionality in Upload Mode
    Removed the 'You are using a cracked binary' GenTool message
    Added Display (Menu) options 19, 20
    Changed Frame Step (O) replay functionality to no longer turn off Fast Forward (F)
    Changed font type and size of Ticker and Event message(s)
    Changed MDS+ Text to no longer show when GenTool Display (Menu) option is turned OFF
    Changed MDS Popup to display player names with player colors
    Changed MDS Popup to longer show button information
    Refactored replay parsing code

-----------------------------------------------------------------------------------------------

New Features 7.9

    Added player rank (*) and experience points (XP) to Money Display
    Enabled Money Display for match observers
    Enabled full access to Camera Height and Camera Pitch feature for match observers

Fixes/Improvements 7.9

    Fixed a crash on game shutdown caused by the use of a DirectX 8 to DirectX 9 wrapper
    Fixed an issue that would show Money Display for players sometimes
    Fixed an issue where Camera Height feature could be exploited in LAN matches
    Fixed non-functional ranked maps if game client was started without internet connection
    Removed observer player entries from Money Display
    Removed cnc-online.net popup message when launching the game with GameRanger
    Reworked GenTool key input manager
    Improved Match Timer to show the actual match progress instead of passed time
    Enabled full compatibility with Zero Hour Contra Mod
    Upload Mode: Fixed a directory issue when uploading files
    Upload Mode: Fixed errors 23, 35, 56 at begin of upload session
    Upload Mode: Increased quality of uploaded images
    Upload Mode: Decreased upload session retry wait time from 120 to 60 seconds
    Upload Mode: Removed minimum replay size limit (5 kb) from upload session
    Upload Mode: Improved contents and formatting of replay text information
    Upload Mode: Improved upload session success message to show full upload url
    Updater: Fixed broken patching of Window.big or WindowZH.big for some game installations
    Updater: Removed broken map.ini from [RANK] Australia ZH v1
    Updater: Disabled installations of ReadMe txt files
    Updater: Implemented new *.dat format for generic patch files

-----------------------------------------------------------------------------------------------

New Features 7.8

    Added new ranked maps to GenTool updater
    Added custom map list size patching to GenTool updater to list up to 1200 maps
    Added player money display in replay and observer modes
    Added text size toggle on [Numpad +] and [Numpad -] keys
    Added player name and player id information in uploaded replay txt file

Fixes/Improvements 7.8

    Fixed tick threading issue that would cause wrong timers
    Changed folder structure in uploaded data to separate online and network matches
    Changed text in GenTool menu
    Improved code in Upload Mode

-----------------------------------------------------------------------------------------------

Fixes/Improvements 7.7

    Fixed a crash when GenTool failed to connect to the Internet
    Fixed a crash in Upload Mode when using certain Mods
    Fixed a bug in Upload Mode where replays would be uploaded with wrong dates

-----------------------------------------------------------------------------------------------

Fixes/Improvements 7.6

    Fixed a bug where a new team selection would not be selectable for 2 seconds
    Fixed a bug in Upload Mode where replays would be uploaded with wrong dates
    Fixed MDS false positive when an infantry unit captures a stealthed abandoned vehicle

-----------------------------------------------------------------------------------------------

New Features 7.5

    Enabled reveal of random colors and start positions with Random Balance
    Enabled extra camera height in singleplayer missions
    Enabled low camera pitch values in singleplayer missions
    CNC Online: Added 2v2 ladder scoreboard and points preview
    Added menu display option to disable ticker/event/ladder displays
    Added support for chain loading other d3d8 files (d3d8x.dll, -proxy custom.dll)
    Added MDS checks for detecting maphack with buildings during replay playback
    Added support for custom camera heights in mods and custom maps (singleplayer, online)

Fixes/Improvements 7.5

    Disabled GenTool initialization in WorldBuilder
    Changed scroll speed value steps in GenTool menu
    Changed the ini file validation to accept the modification of a variety of UI related files
    Improved the screenshot feature to allow taking images without limitations (F11 button)
    Improved Anti Cheat
    Improved MDS performance
    Improved internal memory management and performance
    Fixed a bug that caused wrong ticker event times for American time zones (UTC-x)
    Fixed potential crashes
    Refactored major parts of code

-----------------------------------------------------------------------------------------------

New Features 7.4

    Added rank support for more custom community maps

Fixes/Improvements 7.4

    Slight performance improvement on custom ranked map validation routine

-----------------------------------------------------------------------------------------------

Fixes/Improvements 7.3

    Fixed GenTool compatibility bug with Generals German2 version
    Fixed an exploit in LAN
    Added 'CNC Online' match mode to Upload Mode text information
    Updater: Fixed a faulty result message in GenTool Updater
    Refactored minor parts of code

-----------------------------------------------------------------------------------------------

Fixes/Improvements 7.2

    Fixed GenTool bug that would crash game on Windows XP
    Fixed Game File Validation incompatibility with Zero Hour ME 2009
    Added 'GameRanger' match mode to Upload Mode text information

-----------------------------------------------------------------------------------------------

New Features 7.1

    Added full GameRanger support for Upload Mode, Ticker and GenTool updater
    Added Fault Tolerant Heap to help against CNC Online match freeze
    CNC Online: Added Point Reward Preview for Shatabrick Ladder
    Anti Cheat: Added a Game File Validator that blocks cheat mods

Fixes/Improvements 7.1

    Fixed critical crash and performance issue from 'Origin In Game'
    Fixed GenTool crash on game shutdown (when run without compat mode)
    Fixed GenTool crash in 'WorldBuilder'
    Added a warning message in the event that 'Origin In Game' is used
    Improved internal Anti Cheat mechanisms
    Removed the game version mismatch message for 'WorldBuilder'
    Removed obsolete terrain draw modes from Render Mode
    Fixed a bug where the ticker would draw an empty message
    CNC Online: Now disables stats if not all players are on proper gentool version (6.6)
    CNC Online: Fixed a bug where the ladder display could stall the lobby
    Updater: Fixed a code bug in the registry lookup of the Maps Patcher
    Updater: Fixed a bug where the updater would re-download GenTool everytime
    Changed 'upload.log' (Unicode) to 'gentool.log' (ANSI)
    Fixed general flaws in code
    Refactored major parts of code

-----------------------------------------------------------------------------------------------

New Features 7.0

    Fixed a series of critical exploits: scud bug, tunnel bug, building bug
    Added a latency presentation as light blue frame count in GenTool HUD
    Updater: Added update package to fix cracked game.dat in Zero Hour

Fixes/Improvements 7.0

    Improved the Unofficial Maps tab in statistics enabled game room to remember map selection
      within current session
    Changed MDS lag tolerance to a minimum of 10 and maximum of 64
    Removed error messages on failed ticker loading
    Added notification on game start to advertise cnc-online.net to GameRanger players

-----------------------------------------------------------------------------------------------

Fixes/Improvements 6.9

    CNC Online: Fixed a crash on log in

-----------------------------------------------------------------------------------------------

New Features 6.8

    CNC Online: Enabled Relax Maps to be playable with statistics from unofficial map list
    CNC Online: Added shatabrick.com top 10 scoreboard(s) to online experience

Fixes/Improvements 6.8

    Fixed a bug where matches were uploaded without date to server
    Refactored GameSpy API to increase lobby performance
    Updater: Added option to cancel installation of third party packages (e.g. Relax Maps)
    Updater: Removed forced RUNASADMIN WINXPSP3 flags
    Updater: Removed Direct Connect Fix for GameRanger users
    Updater: Improved Updater functionality for UAC enabled systems

-----------------------------------------------------------------------------------------------

Fixes/Improvements 6.7

    Fixed black screen issue in The First Decade - Generals Zero Hour

-----------------------------------------------------------------------------------------------

New Features 6.6

    CNC Online: Added access to Generals Statistics server
    CNC Online: Added exclusive access to Shatabrick.com Generals Ladder
    Updater: Added Generals version mismatch fix for The Ultimate Collection (Origin)
    Updater: Added Relax Maps as downloadable package for Zero Hour users
    Updater: Added direct connect fix for non-English GameRanger users
    Updater & Installer: Added force set of RUNASADMIN WINXPSP3 on generals.exe for
      Windows Vista and higher

Fixes/Improvements 6.6

    Fixed a bug where the resolution text was not printed correctly on uploaded images
    Improved GenTool Updater

-----------------------------------------------------------------------------------------------

New Features 6.5

    Added game access to cnc-online.net servers (Revora)
    Added Auto Updater

Fixes/Improvements 6.5

    Fixed a crash when launching a Generals Campaign mission
    Fixed cursor lock with dual monitor setup
    Adjusted UI content

-----------------------------------------------------------------------------------------------

Fixes/Improvements 6.4

    Fixed potential crash on application shutdown
    Fixed MDS issue where repairing unit on stealthed building caused false positive
    Moved GenTool save folder from C:\GenTool to C:\Users\...\Documents\GenTool
    Removed Upload Mode text when inside match or replay
    Removed version specific Ticker messages
    Changed Upload Mode to work with latest game versions only
    Changed Screenshot folder name from "Shots" to "Images"
    Changed maximum JPG quality setting from 100 to 95
    Changed Lobby FPS to 20 when windowed game is not in foreground
    Reworked game version authentication

-----------------------------------------------------------------------------------------------

Fixes/Improvements 6.3

    Fixed Windows 8 Crash (apphelp.dll)
    Fixed Origin crash from Origin Game Overlay (IGO32.dll)
    Fixed upload bug where screenshot was taken twice
    Fixed non-unique upload player ID in folder name
    Fixed some exploits in Upload Mode
    Added fixes to prevent local zoom hack in LAN matches
    Added check for GameRanger to hide GenTool intro screen (-nologo startup parameter)
    Changed GenTool's brightness behavior
    Refactored key management
    Improved memory usage

-----------------------------------------------------------------------------------------------

Fixes/Improvements 6.2

    Improved Fix for ScudBug
    Improved time management for Upload Mode and other clock dependent features
    Removed force time update on Windows clock
    Added incompatible version warning to Generals - The Ultimate Collection
    Improved some code

-----------------------------------------------------------------------------------------------

New Features 6.1

    Added full support for Generals and Zero Hour from The Ultimate Collection
    Added code fixes for Scud Bug
    Added code fixes for Multiplayer Crash when many units are in movement

Fixes/Improvements 6.1

    Fixed potential crash when Player Table was opened
    Fixed broken online nickname warnings
    Fixed broken MDS in Generals
    Changed Player Table layout
    Minor code optimizations

-----------------------------------------------------------------------------------------------

Fixes/Improvements 6.0

    Reverted some MDS changes from version 5.9
    Improved MDS techniques

-----------------------------------------------------------------------------------------------

New Features 5.9

    Added MDS Profiler/Output
    Added Brightness option to GenTool menu

Fixes/Improvements 5.9

    Fixed "GetLogicalProcessorInformation" crash for Windows XP SP2 and lower
    Fixed "unknown" upload directory in CCG upload mode
    Fixed blurry GenTool image on boot up
    Changed Config Save to keep settings accross new versions
    Increased overall MDS performance
    Improved MDS detection accuracy by scanning player actions
    Improved MDS AIState retrieval strategy
    Improved Scrolling behaviour when the Spetating mode or MDS Popup is active
    Improved Event readability by converting UTC to local times
    Improved Event logic
    Refactored/Optimized code for readability and efficiency

-----------------------------------------------------------------------------------------------

Fixes/Improvements 5.8

    Fixed major performance issues by improving Single Core and Multi Core CPU techniques
    Fixed missing plane lock warning in CCG MDS
    Fixed missing deletion of left over files from Upload Mode on application quit during
      active upload
    Added deactivation of scroll while player spectating is enabled in Replay mode

-----------------------------------------------------------------------------------------------

New Features 5.7

    Added event scheduler in main menu

Fixes/Improvements 5.7

    Fixed possible crash when MDS was used with replays containing non-ASCII player names
    Fixed MDS false positive due lock under fog on buildings
    Fixed MDS false positive due workers clearing mines
    Fixed MDS false positive due angry mobs auto locking units in fog (added warning)

-----------------------------------------------------------------------------------------------

Fixes/Improvements 5.6

    Fixed Error 45, Error 13 and similar on game launch/exit
    Fixed MDS false positives due attack-move with high range and similar moves
    Fixed user error with wrong windows clock DST settings by increasing clock adjustment
      tolerance to 1:05 hours
    Fixed upload.log not being saved to correct location when storage was changed
    Added modding support by allowing custom camera height settings from GameData.ini
    Added popup on game start if game version is not fully supported
    Changed texts
    Changed major parts of code

-----------------------------------------------------------------------------------------------

Fixes/Improvements 5.5

    Fixed mismatch bug from previous GenTool version
    Fixed potential crash with replay observer
    Fixed non closing MDS popup when pressing P without player selected
    Minor code changes

-----------------------------------------------------------------------------------------------

New Features 5.4

    Fixed multiple game crashes when selecting players in replay mode
    Fixed game crash when leaving replay mode while player places beacon
    Added Fog of War in replay mode to see match through player's eyes
    Added Auto Focus Spectating of observed player (ZH only)
    Added Auto Camera height adjustment in Replay when fog enabled
    Added Maphack Detection System in replay mode
    Added Frame Stepping for Pause in replay mode
    Added automatic Windows Clock adjustment if it is set wrong on game start

Fixes/Improvements 5.4

    Fixed rare crash on game start caused by Upload Mode
    Fixed uploading issue when using # in LAN nickname
    Fixed text size issue when using Windows DPI higher than 100%
    Fixed replay match timer inaccuracies when using fast forward
    Fixed minor issue with ControlBar toggle in replay mode
    Fixed minor issue in font class
    Added cracked game.dat to be compatible (ZH only)
    Added version recognition in PlayerTable with older GenTool versions
    Added note about match location in Replay Information text file
    Changed match length time format in Replay Information text file
    Changed storage device options to decrease user mistakes
    Changed lobby ping appearance of non GenTool players
    Changed GenTool menu appearance and colors
    Changed appearance of lobby nickname warnings as popups
    Changed appearance of game timer
    Removed [Pause] key of replay pause but kept [P]
    Reworked major parts of code

-----------------------------------------------------------------------------------------------

Fixes/Improvements 4.7

    Fixed crash when files failed uploading
    Fixed missing GenTool root folder creation leading to failing uploads/screenshots
    Fixed storage device not being saved to config

-----------------------------------------------------------------------------------------------

Fixes/Improvements 4.6

    Fixed 3 potential crashes
    Fixed several config exploits
    Fixed minor intro screen text issue
    Fixed minor nickname warning message issue
    Fixed window position issue from using left/top windows taskbar
    Added new upload server at www.gentool.net
    Added Upload Mode status to Player Table
    Added frame counter to replay timer
    Added new messages in upload.log in case of upload failures
    Excluded Observers from turning off Random Balance
    Excluded Observers from uploading screenshots
    Changed render mode shader
    Changed minor aspects of menu
    Changed Player Table making it incompatible with older versions incl. Random Balance
    Decreased upload screenshot quality
    Improved upload security
    Improved upload pipeline
    Improved upload status output
    Improved replay save to store broken replays
    Improved ticker system
    Improved major parts of code
    Removed game freeze when quitting game during active upload
    Removed ticker message when using WorldBuilder

-----------------------------------------------------------------------------------------------

Fixes/Improvements 4.2

    Fixed possible crash on game launch
    Fixed crash when toggling Camera Rotation in Generals
    Fixed Resolution Lock & Cursor Lock toggle issue on Windows 7
    Fixed Replay Speed & Camera Rotation settings not restored correctly from config file
    Fixed logic issue with Camera Pitch toggle keys
    Fixed minor issues in Screenshot procedure
    Improved performance of 16 Bit screenshot procedure
    Improved Window Position to consider windows taskbar and 2nd monitor
    Improved code
    Renamed default Fps Limit value to "Default"
    Removed Camera Pitch numpad key toggle in lobby
    Removed Camera Pitch toggle when replay is paused
    Added alternative Pause button on P-key
    Added upward limit for Scroll Speed option

-----------------------------------------------------------------------------------------------

New Features 4.1

    Added Camera Rotation speed option
    Added Replay Fast Forward speed option
    Added Replay Pause button
    Modified Replay Game Timer to display the real runtime
    Modified Replay Camera Pitch to allow changes down to 16.5
    Added Replay Camera Extra option to increase camera height and draw entire terrain
    Added Replay Render Mode with wireframes and polycounts
    Added more FPS limit options
    Added file size HUD information when taking screenshot
    Removed Game Controls input when opening GenTool menu in game mode
    Added Escape for quit and Return for toggle as alternative GenTool menu buttons
    Added minor visual changes to GenTool menu

Fixes/Improvements 4.1

    Fixed crash when launching game with GameRanger
    Fixed missing upload retry upon failed fileserver connection
    Fixed HUD/Ticker overlapping GenTool menu
    Fixed Camera Pitch issues in Replay
    Fixed Camera Pitch issues in Shellmap
    Fixed Map scroll bounding issue upon camera change
    Fixed taking Screenshot in WorldBuilder when window not active
    Reworked GenTool key technology to avoid issues with other programs using GetAsyncKeyState
    Improved Ticker rendering to avoid warping text during loading times
    Improved GenTool menu code
    General code refactoring

-----------------------------------------------------------------------------------------------

Fixes/Improvements 3.3

    Fixed WorldBuilder incompatibility issues
    Added 16-bit Color mode support for screenshots
    Fixed rare gentool menu rendering issue
    Fixed flaw in Player Table to avoid rare false detection
    Fixed "null" string in upload.log
    Improved screenshot messages
    Fixed minor replay file naming issue when observer was involved
    Improved some code

-----------------------------------------------------------------------------------------------

Fixes/Improvements 3.2

    Fixed rare upload mode hang
    Increased internet time request from 5 to 10 seconds to help avoiding wrong dates on
      Fileserver
    Fixed mistake in ID generation code to avoid multiple and duplicate ID's

-----------------------------------------------------------------------------------------------

Fixes/Improvements 3.1

    Fixed rare ALT+Tab crash
    Fixed crash from wrong FPS rate
    Fixed broken scroll speed
    Fully reworked widescreen, camera pitch and scroll speed

-----------------------------------------------------------------------------------------------

New Features 3.0

    Improved Anti cheat for local maphack detection
    Added GenTool menu (press Insert)
    Added live GenTool detection in GameSpy lobby
    Added player table to see UID, GenTool, etc in GameSpy lobby
    Added random balancing feature
    Unlocked all supported resolutions in options menu
    Added option to forbid resolution changes
    Added warning if GameSpy nickname is bugged or bad
    Added new upload modes to keep/delete local files
    Changed intro layout and added skip button
    Added window mode repositioning after resolution change
    Added more text size options
    Added JPG quality option for F11 shots
    Added log file for upload transactions in GenTool root folder
    Removed unnecessary toggle keys
    Many other tweaks and improvements

Fixes/Improvements 3.0

    Fixed memory leak
    Fixed rare crash from upload mode
    Avoid double and duplicate ID's for upload folder
    Removed useless underscore in replay name
    Removed upload retry if connection was not established to file server
    Improved CPU performance of text
    Added library to avoid possible XP incompatibility issue
    Fixed broken F9 menu toggle
    Fixed failing uploads when previous upload session failed
    Fixed camera bug

-----------------------------------------------------------------------------------------------

New Features 2.1

    Added full Generals 1.8 support
    Added full The First Decade support
    Added observer chat
    Added configuration save
    Upload Mode: Added RepInfo detection
    Upload Mode: Now attempts to re-upload files to server
    Upload Mode: Exchanged UID from upload name with unique ID
    Upload Mode: Changed replay folders to Game\Month\Day\Player
    Anti Cheat: Shuts down the game when cheat was detected
    Changed camera pitch limit

Fixes/Improvements 2.1

    Upload Mode: Added unicode support to solve path problems
    Upload Mode: Bad player name will now be replaced with "player"
    Upload Mode: {0} in player name will now be removed
    Upload Mode: Now uses system drive as storage location
    Upload Mode: Now uses internet time to avoid wrong date on file server
    Improved general performance
    Fixed crash on game shutdown
    Fixed many code issues

-----------------------------------------------------------------------------------------------

New Features 1.8

    Added Upload mode (serves as anti cheat)
    Added JPG screenshot (F11)
    Added camera pitching (PgDown/PageUp keys)
    Added scroll speed adjustment (+/- keys)

Fixes/Improvements 1.8

    Improved Ticker
    Fixed many minor issues

-----------------------------------------------------------------------------------------------

New Features 1.4

    Added widescreen resolution support
    Added toggle for menu Bar in replay mode

Fixes/Improvements 1.4

    Fixed issue preventing all text not showing up
    Fixed issue showing ticker with failing content

-----------------------------------------------------------------------------------------------

Initial Release (1.0) Features

    Added clock display
    Added match and replay Timer
    Added news ticker
    Added FPS limiter
    Added window positions presets for windowed mode
    Added cursor lock for windowed mode and two monitor setups
