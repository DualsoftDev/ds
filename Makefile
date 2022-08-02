.ONESHELL: # Applies to every targets in the file!


# 다음 명령어를 통해서 submodule 추가 함.
# git submodule add git@dualsoft.co.kr:/git/dual.common.git

submodule:
	git submodule update --init
	git pull --recurse-submodules
