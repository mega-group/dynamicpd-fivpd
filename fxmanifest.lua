fx_version 'cerulean'
game 'gta5'

author 'Mega Utilities\' Mega Group'
description 'This resource checks for updates for the callout & plugin dynamicpd'
version '1.0.4'
pre_version '1.0.3'


server_script 'updater.lua'

changelog_1_0_4 [[
- Updater will now be required
- Fix automatic discovery
- Fix dynamicpd not working (issue due to dynamicdiscovery and namespace most likely)
]]

changelog_1_0_3 [[
- Enhance Updater once more (prepare for FivePD V2?)
- Fix wrong URL in Updater
- 1.0.2 sunsetted due to Updater issues
]]

changelog_1_0_2 [[
- Use fxmanifest in FivePD to load files
- Enhance logging and added more debug options to set in config (printToConsole & debugToConsole)
- Added a event in the updater for server-side console printing
- Enhanced updater
- complete rename to dynamicpd
- & more
]]

changelog [[
- Rebrand to dynamicpd
- Possible fix for cleanup bug (https://github.com/mega-group/dynamicpd-fivpd/issues/1#issuecomment-3272407943)
]]


