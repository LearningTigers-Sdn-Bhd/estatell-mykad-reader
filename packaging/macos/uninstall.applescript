on run
	display dialog "Remove Estatell MyKad Reader from this Mac? Estatell will stop reading MyKad cards on this computer." with title "Uninstall Estatell MyKad Reader" buttons {"Cancel", "Uninstall"} default button "Cancel" cancel button "Cancel" with icon caution
	do shell script quoted form of "/Library/Application Support/Estatell/MyKadReader/uninstall.sh" with administrator privileges
	display dialog "Estatell MyKad Reader is removed." with title "Uninstall Estatell MyKad Reader" buttons {"OK"} default button "OK"
end run
