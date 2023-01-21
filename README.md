# Magnet-Run-3D

[Video Demo](https://www.youtube.com/watch?v=FrG3asgh3Uo)  
[Playable Game](https://tradam.itch.io/magnet-run-3d)  

Magnet Run 3D is a tech demo I developed to exercise and practice the 3Cs of game design/development.
In this demo you play as a ball that rolls around various objects that have complex gravity sources.

<p align="center">
  <img src="https://user-images.githubusercontent.com/11139432/213840115-59413e96-1c22-4229-aada-c59ec042fc1f.png" width="50%"/>
</p>

- All variables which determine the mechanics of the Character(animation speeds, textures, etc), Camera(distances, angles, interpolation speeds, camera target, etc), and Control(jump height, movement speed, gravity, etc) are exposed in the Unity editor/inspector so that game designers could easily tweak and modify the mechanics and game feel.

## Character
- The visuals of the ball and the physics of the ball are modularized and seperated.
  - Physics component of the ball determines position, speed, collisions, etc.
  - Visual component of the ball determines orientation, rolling animations, etc.
    - Calculates the visual rolling speed from the physics component(its movement speed).
    - Gets average of floor normals to determine correct orientation(such as when wall riding or rolling on 2 surfaces at the same time).
    - Uses quaternions to interpolate to the desired orientation from current orientation(in the case of steering the ball).
  - This means that the visual component can be completely replaced by a different visual model(such as a humanoid or creature) and set of animations with ease without distrupting the mechanics of the character.

## Camera
- Orbit camera that follows the player character automatically
  - Automatically determines the direction the player is moving in and smoothly orients itself to face that direction.
  - Detects the direction of gravity on the character to calculate and set the correct orientation.
  - Smoothly interpolates between the current position and the desired position(such as when the direction of gravity changes).
  - Performs a box cast between the proposed camera location and the character to determine if the view would be blocked or if the camera is inside any geometry. If so it will place the camera in front of the blocking geometry.

## Control
  - Input is converted to be relative to the camera and direction of gravity allowing for intuitive movement.
  - Movement speed on the ground and in the air is split into 2 variables allowing better tuning of game feel.
  - When moving over an edge, a raycast check is performed and if the slope is small enough the the character sticks the surface rather then flying off the edge.
