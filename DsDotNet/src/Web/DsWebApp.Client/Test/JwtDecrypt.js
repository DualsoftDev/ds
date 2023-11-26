
const token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6ImFkbWluIiwicm9sZSI6IkFkbWluaXN0cmF0b3IiLCJuYmYiOjE3MDA4NTY5MDEsImV4cCI6MTcwMDg1ODEwMSwiaWF0IjoxNzAwODU2OTAxfQ.PLQjhNiEc_nSaqwkqALV-ZcEio6Z-tnNUXTki_ZTgm0";
const parts = token.split('.');
const header = JSON.parse(atob(parts[0]));
const payload = JSON.parse(atob(parts[1]));
console.log(payload);



const base64Url = token.split('.')[1];
const base64 = base64Url.replace('-', '+').replace('_', '/');
const json = JSON.parse(atob(base64));
console.log(json);
