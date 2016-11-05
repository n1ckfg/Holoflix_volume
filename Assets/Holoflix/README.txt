

*****  HOLFLIX  *****

An example of how to use/view a STRUCTURE SENSOR recording in Unity3D.

Holoflix is designed to be used in conjunction with the 'Hypercube: Volume Plugin' but it can just as
easily be used independently to view volumetric data from the Structure Sensor.


TO USE:

1) Import your video into Unity
2) Select the Holovid game object
3) Find the material assigned to it in it's inspector.
4) Apply your video to the materials texture
5) On that material, choose what kind of Holoflix > shader you want it to render with (each documented below)
5) play the scene!  ^_^

* Toggle the GUI with space bar
* If the displacement is too high or too low you can adjust it either in said material, or in the GUI slider during play.


HOLOFLIX SHADERS:

-- Holoflix
A simple opaque plane that is deformed based on the depth from the video and the settings.

-- Holoflix Particle
Instead of a distored opaque plane, opaque quads are used to represent each point (like a particle). 
You can choose if they are UVed quads or not.

-- Holoflix Particle Additive
A quad represents each point.  You can choose if they are UVed quads or not.
Blending is additive so that quads are visible behind each other. 
You can also choose to overlay an image over each quad to selectively mask parts of it.

-- Holoflix Particle Cutout
An opaque quad represents each point.  You can choose if they are UVed quads or not.
You can add a separate image will can be used to perform alpha test masking on every quad.

* Every shader has an option for extrusion distance and forced perspective.
* the soft slicing option is meant to be used with the Hypercube: Volume Plugin which chops the Holoflix mesh into
slices for rendering.  This option lets the slices be blended.

