"use strict";

function fitCrystal() {
    const crystals = document.querySelectorAll('.crystal');
    const ratio = 1 / 3; // width / height

    crystals.forEach(crystal => {
        const parent = crystal.parentElement;

        const parentWidth = parent.clientWidth;
        const parentHeight = parent.clientHeight;

        // Compute the ideal size if width were 100%
        const heightIfFullWidth = parentWidth / ratio;
        const widthIfFullHeight = parentHeight * ratio;

        // Choose which one fits
        if (heightIfFullWidth <= parentHeight) { // cell is tall and narrow, crystal is 1/rt(2) * width (full width when rotated 45deg)
            crystal.style.width = '100%';
            crystal.style.height = `${heightIfFullWidth}px`;
            document.documentElement.style.setProperty("--crystal-max-width", "70.6%");
        } else {
            crystal.style.width = `${widthIfFullHeight}px`;
            crystal.style.height = '100%';
            if (widthIfFullHeight * Math.sqrt(2) <= parentWidth) { // cell is short and wide, crystal is full height and 1/3 of height in width. Continuing to widen cell has no effect on crystal size

                document.documentElement.style.setProperty("--crystal-max-width", "100%");

            }
            else { // cell is not yet wide enough for full height, .crystal width is clamped, actual crystal can continue to widen (hence lengthen), taking into account width when rotated 45deg.
                var test = 70.6 / (widthIfFullHeight / parentWidth); // widen crystal until .crystal width / .scene width = 1/rt(2), and we pass the 'if' block
                document.documentElement.style.setProperty("--crystal-max-width", `${test}%`);
            }
        }
    });
}

// run on load + resize
window.addEventListener('resize', fitCrystal);
window.addEventListener('load', fitCrystal);