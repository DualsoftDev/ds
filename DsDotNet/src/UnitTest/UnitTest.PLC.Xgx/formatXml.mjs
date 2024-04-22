/*
 * Xml formatting 을 위한 javascript 파일
 * - 설치
 *      npm install xml-formatter
 * - 실행
 *     node formatXml.mjs < input.xml > output.xml
 */
import xmlFormat from 'xml-formatter';

// 표준 입력 설정
process.stdin.setEncoding('utf-8');

let inputData = '';

// 데이터가 stdin으로 들어올 때 이벤트 리스너
process.stdin.on('data', (chunk) => {
    inputData += chunk;
});

// stdin이 종료되면 (EOF) 실행
process.stdin.on('end', () => {
    try {
        const formattedXML = xmlFormat(inputData);
        process.stdout.write(formattedXML);
    } catch (error) {
        console.error('XML 포맷 에러:', error.message);
    }
});

