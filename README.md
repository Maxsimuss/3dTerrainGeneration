# 3dTerrainGeneration

Rendering a procedurally generated voxel-mesh world with "modern" opengl leveraging MultiDrawIndirect (see /Engine/Graphics/3D/SceneRenderer.cs).

Chunk data is stored in a voxel octree for lower memory usage on simple terrain (/Engine/Util/Ocree.cs).
Meshes are generated using a greedy algorithm pasted from somewhere.
Reads .vox files using https://github.com/sandrofigo/VoxReader.

Heavily relies on TAA for antialiasing and denoising the 2 sample per pixel ssao implementation from learnopengl.com
TAA is expensive (1.1ms on GTX980@4k) running at full res with r11g11b10f & r32f textures going in. 
32bit float is used to store previous fragment depth for depth based rejection.
Not sure if this concept has been used before, but combined with color rejection it gets rid of most ghosting artifacts 😄.

Using persistently mapped buffers for 2d rendering in /Engine/Graphics/UI/UIRenderer.cs for fun.
It will cause problems later though...

Also had some fun with Global Illumination in /shaders/rtgi.frag
It doesn't use any acceleration whatsoever and needs to be ran at 16th of the resolution for any usable performance.
/shaders/lighting.frag uses upscaling starting line 76 to prevent GI leaking between objects due to low resolution.
It uses position and normals to find the closest GI sample for the current fullres pixel being lit.