Unity Infinite Isometric Terrain Generator
=================================

**Test in your browser: [https://zulfajuniadi.github.io/isometric-land-generator/](https://zulfajuniadi.github.io/isometric-land-generator/)**

> **Minimum required version is Unity 2018.3**. Will not work on older versions.

[![Endless Terrain](https://i.gyazo.com/c23baacdebd93f8f02caa634cabcf5df.gif)](https://gyazo.com/c23baacdebd93f8f02caa634cabcf5df)

[MP4 Video](https://gyazo.com/c23baacdebd93f8f02caa634cabcf5df)

## Features:

1. Generates infinite terrain
2. Uses the new Unity isometric tilemap
3. Tool included to create your own terrain set
4. Near-zero allocation (after all active tilemaps has been generated)
5. Uses `Graphics.DrawMeshInstancedIndirect` to draw ~~a crap ton~~ lots of trees at once
6. Recycles inactive chunks
7. Simple randomized spawners (for trees, shrubs, etc)
8. Fast terrain generation (< 16 ms per chunk)
9. Rendering the whole map with trees inside the editor takes 3ms with vSync off. Should be less than that on a built player.
10. Unsorted (fast) and Sorted (3x slower) tree rendering
11. WebGL Support

## Quick Start:

Open up the Example scene inside the Example folder and press play 

## Settings:

[![Settings](https://i.gyazo.com/b76ce447800d593d1438cad92a83d920.png)](https://gyazo.com/b76ce447800d593d1438cad92a83d920)

1. Seed: The generator seed
2. Height: The maximum height of the terrain
3. Noise Scale: Flatness of the terrain
4. Active Tilemaps: How many chunks are active at once
5. Biome configs: The configuration of the biome
6. Terrain Curve: The curve to be used when interpolating the results of the perlin noise sample
7. Rendering mode: Unsorted (fast) and Sorted (3x slower) spawner items rendering, WebGL only supports Sorted Rendering
8. Auto Generate: Detect changes inside the editor and auto generates tiles

## Biome Configs:

[![Biome Configs](https://i.gyazo.com/8ea76d7b586b2135660135bd44616103.png)](https://gyazo.com/8ea76d7b586b2135660135bd44616103)

1. Tile config: The tile configuration
2. Height: 0-1 `float` value of the height of this biome
3. Spawners - Spawner: The Spawner config
4. Spawners - Probability: The probability of the spawner being spawned

## Tile Config:

[![Tile Configs](https://i.gyazo.com/2c57989243fe668cbdf5b77a58107085.png)](https://gyazo.com/2c57989243fe668cbdf5b77a58107085)

A list of TileBase being used to generate the terrain. The config is read clock wise from the top corner. For example 1110, would be:

- 1 tile up on the top corner
- 1 tile up on the right corner
- 1 tile up on the bottom corner
- 0 tile up on the left corner

Which you would set to this:

[![1110](https://i.gyazo.com/4301a16de541fa600894a06bfc02f119.png)](https://gyazo.com/4301a16de541fa600894a06bfc02f119)

It can be counter intuitive at times but it makes sense once you see it generated.

As the field type is `TileBase` you can extend the `Tilemap.Tilebase` class to create your own tiles. I've downloaded the `AnimatedTile` class from [Unity's 2D Extras repository](https://github.com/Unity-Technologies/2d-extras/).

## Spawner Config:

[![Spawner Config](https://i.gyazo.com/7801fc7c2c932d4eee77a2d1a861b19d.png)](https://gyazo.com/7801fc7c2c932d4eee77a2d1a861b19d)

The instanced renderer takes a list of 2D sprites that are packed into a 3D texture.

1. Sprites: The sprites you want to pack
2. Packed Texture: The result 3D texture
3. Mesh Size: The size of the generated Mesh on the map
4. On Flat: If this is true, then the sprite will be spawned on flat terrain.
5. On Slopes: If this is true, then the sprite will be spawned on slopes (like the bamboo around the lake).
6. Enabled: If this is unchecked, it will not be rendered
7. Click on `Generate Packed Image` button if you add / remove / update the sprites list. That will generate a new 3D texture and assign it to the `Packed Texture` field.

## Tile Generator Tool

> Requires Blender

The tile genertor tool is a Blender file that takes a texture and exports terrain sprites to be used inside this terrain generator. It is located at  `Assets/Example/Terrain/Tile Generator Tool.blend`.

1. Create a `.png` image named `texture.png` in the same directory of the blend file
2. Double click the `Tile Generator Tool.blend` to open up the blender file
3. Change the `Output` directory and file name

[![Output Directory](https://i.gyazo.com/9f8da7c3a4fcf64228ad63118209c875.png)](https://gyazo.com/9f8da7c3a4fcf64228ad63118209c875)

4. Click on the `Animation` 

[![Animation](https://i.gyazo.com/4df06d853db9d7e2375906b13a2f6b9b.png)](https://gyazo.com/4df06d853db9d7e2375906b13a2f6b9b)

5. New textures will be generated according to the output directory that you set 

[![Image from Gyazo](https://i.gyazo.com/6d3cde69ab042ae0b98763a7f8870e81.png)](https://gyazo.com/6d3cde69ab042ae0b98763a7f8870e81)

> **It is important to create the texture first before running the tool**


## Texture Import Settings

Once you've created the tiles, you'll have to import them as sprites with these settings:

[![Import Settings](https://i.gyazo.com/a0b1082c7ca7514b4cbc1d01c660f5cb.png)](https://gyazo.com/a0b1082c7ca7514b4cbc1d01c660f5cb)

1. Texture Type: Sprite
2. Sprite mode: Single
3. Pixels Per Unit: 63

Some tiles will need a custom pivot to look right on the map. Refer to the import settings on the examples tiles I've already created.

## Future improvements 

Pull requests are welcomed :)

1. Tile map tools such as grid management and pathfinding so that this can actually be used in a game.
2. Terraforming
3. Prefab spawning
4. Animals

## Credits

1. [Nature Kit by Kenny](https://kenney.nl/assets/nature-kit)
2. [Terrain Renderer by Clint Bellanger](https://opengameart.org/content/terrain-renderer)
3. [Trees Megapack as curated by rrexky](https://opengameart.org/content/trees-mega-pack-cc-by-30-0)
4. [Open Game Art](https://opengameart.org)
