// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function changeFeature(type, element)
{


document
.querySelectorAll(".feature-tab")
.forEach(tab=>{

tab.classList.remove("active");

});


element.classList.add("active");



let icon =
document.getElementById("featureIcon");


let title =
document.getElementById("featureTitle");


let description =
document.getElementById("featureDescription");



let content =
document.querySelector(".feature-content");



content.classList.remove("content-change");



setTimeout(()=>{


if(type==="flat")
{

icon.innerHTML="🏠";

title.innerHTML="Flat Management";

description.innerHTML=
"Manage flats, residents and communities from one powerful management platform.";

}



if(type==="notice")
{

icon.innerHTML="📢";

title.innerHTML="Smart Notices";

description.innerHTML=
"Send important announcements and alerts instantly to residents through a centralized system.";

}



if(type==="billing")
{

icon.innerHTML="💳";

title.innerHTML="Digital Billing";

description.innerHTML=
"Automate rent collection, utility invoices, and instant digital receipts effortlessly.";

}



content.classList.add("content-change");


},100);


}

document
.getElementById("loginForm")
?.addEventListener("submit",function(){


document.getElementById("loginText")
.style.display="none";


document.getElementById("spinner")
.style.display="inline-block";


});
