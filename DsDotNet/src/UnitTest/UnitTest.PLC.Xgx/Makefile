clean:
	rm -rf xgi/Xmls/*.xml xgk/Xmls/*.xml
	rm -rf xgi/Xmls/*.state xgk/Xmls/*.state

confirm:
	-cp xgi/Xmls/*.xml xgi/Xmls/Answers/
	-cp xgk/Xmls/*.xml xgk/Xmls/Answers/
yes: confirm
no:
	git restore xgi/Xmls/Answers xgk/Xmls/Answers