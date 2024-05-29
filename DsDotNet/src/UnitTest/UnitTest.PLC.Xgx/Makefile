clean:
	rm -rf xgi/Xmls/*.xml xgk/Xmls/*.xml
	rm -rf xgi/Xmls/*.state xgk/Xmls/*.state
	rm -rf xgi/Xmls/Answers/*.state xgk/Xmls/Answers/*.state

confirm:
	-cp xgi/Xmls/*.xml xgi/Xmls/Answers/
	-cp xgk/Xmls/*.xml xgk/Xmls/Answers/
yes: confirm
no:
	(cd xgi/Xmls/Answers; git restore .)
	(cd xgk/Xmls/Answers; git restore .)
