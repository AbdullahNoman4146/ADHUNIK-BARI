function toggleCard(card)
{

card.classList.toggle("active");

}




// ============================
// COUNTER ANIMATION
// ============================


function animateCounter(counter)
{


let target =
parseInt(counter.dataset.target);



let duration = 1500;


let start = 0;


let startTime = null;



function update(time)
{


if(!startTime)

startTime=time;



let progress =
(time-startTime)/duration;



let ease =
1-Math.pow(1-progress,4);



let value =
Math.floor(
ease * target
);



counter.innerHTML=value;



if(progress < 1)
{

requestAnimationFrame(update);

}

else
{

counter.innerHTML=target;

}


}



requestAnimationFrame(update);


}






// ============================
// OBSERVER
// ============================


const statsObserver =
new IntersectionObserver((entries)=>{


entries.forEach(entry=>{


if(entry.isIntersecting)
{


document
.querySelectorAll(".counter")
.forEach(counter=>{


if(!counter.classList.contains("counted"))
{


counter.classList.add("counted");


animateCounter(counter);


}


});


}


});


},
{
threshold:.5
});





const stats =
document.querySelector(".metrics-bar");


if(stats)

statsObserver.observe(stats);






// ============================
// CARD REVEAL
// ============================



const cardObserver =
new IntersectionObserver((entries)=>{


entries.forEach(entry=>{


if(entry.isIntersecting)
{


entry.target.classList.add("show");


}


});


},
{

threshold:.2

});





document
.querySelectorAll(".reveal-card")
.forEach(card=>{


cardObserver.observe(card);


});