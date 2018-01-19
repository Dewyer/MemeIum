
$("#privkey").change(privChanged);

function hexToBase64(hexstring) {
    return btoa(hexstring.match(/\w{2}/g).map(function (a) {
        return String.fromCharCode(parseInt(a, 16));
    }).join(""));
}

function privChanged() {
    var outp = $.parseXML($("#privkey").val());
    var ses = $(outp);
    var mod = ses.find("Modulus").text();
    var exp = ses.find("Exponent").text();
    var md = forge.md.sha256.create();
    md.update(exp + " " + mod);
    var addr = hexToBase64(md.digest().toHex());
    console.log(addr);
    $("#addr").val(addr);
}