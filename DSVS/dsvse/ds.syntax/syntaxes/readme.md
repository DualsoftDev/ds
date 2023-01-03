# YAML to json with variable


1. install yamlsyntax2json

npm i -g com.matheusds365.vscode.yamlsyntax2json


2. Use variables

variables:
  systemHeaders: '(sys|system)'
  ipSpecHeaders: '((ip|host)\s*=\s*[가-힣a-zA-Z_0-9]*)?'

3. type to convert yaml to json

yamlsyntax2json ds.YAML-tmLanguage ds.tmLanguage.json