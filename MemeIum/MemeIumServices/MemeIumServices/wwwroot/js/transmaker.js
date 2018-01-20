var dick = function ()
{
    console.log("my dick");
    var newIndex = Math.max(...vouts)+1;
    vouts.push(newIndex);
    $("#vouts").append(getNewListElemntCode(newIndex));
    console.log(vouts);
}

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

function getNewListElemntCode(ii) {
    var out = "<li class='list-group-item' id='listi"+ii+"'>";
    out += "<div><button type='button' class='btn btn-danger' onclick='delListItem(" + ii + ")'>X</button>";
    out += "<label for='toaddr" + ii + "'>To Address:</label>";
    out += "<input type='text' class='form-control' id='toaddr" + ii + "'>";
    out += "<label for='ammount" + ii + "'>Ammount:</label>";
    out += "<input type='number' class='form-control' id='ammount" + ii + "'></div>";
    return out;
}

function delListItem(ii) {
    $("#listi" + ii).remove();
    var index = vouts.indexOf(ii);
    vouts.splice(index, 1);
}

var vouts = [1];

$(function () {
    $("#privkey").change(privChanged);
    //$("#adder").on("click", function () { dick() });

});
