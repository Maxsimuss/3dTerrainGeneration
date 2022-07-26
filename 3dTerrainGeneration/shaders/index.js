for (let i = 0; i < 6; i++) {
    let ang = i / 3 * Math.PI;
    console.log("vec2(" + Math.sin(ang) + ", " + Math.cos(ang) + "), ");
}

for (let i = .5; i < 6.5; i++) {
    let ang = i / 3 * Math.PI;
    console.log("vec2(" + Math.sin(ang) / 2 + ", " + Math.cos(ang) / 2 + "), ");
}