# AltMapIconRendererContinued

An advanced client-side mod for Vintage Story that enhances the visual experience of player and waypoint icons on the map.

---

## Features

- **Pinned Player Icons**  
  Player icons can optionally "pin" to the edge of the map and minimap. 

- **Customizable Waypoint Appearance**
  - Switch between circular or square-shaped icons.
  - Toggle thick or thin outline styles.
  - Adjust icon scale to fit your zoom preference.
  - Hide or show icons dynamically.

- **Player Icon Color Customization**
  - Change the color of player map icons.
  - Toggle outline thickness or style.

- **Vinconomy Compatibility**
  - Fully integrates with Vinconomy shop waypoints.
  - Your size, outline, shape, and hover settings apply consistently.

---

## Commands

All commands use the prefix `.amir`.

| Command                 | Description                                             |
|------------------------|---------------------------------------------------------|
| `.amir help`           | Show usage instructions                                 |
| `.amir square on/off`  | Toggle square-shaped waypoint icons                     |
| `.amir outline thin/thick` | Choose outline thickness for icons              |
| `.amir pc #hexcolor`   | Set player icon tint color (e.g., `#FF0000`)            |
| `.amir size 1.0`       | Set icon scale (e.g., `0.5`, `1.5`, `2.0`, etc.)         |
| `.amir pin on/off`     | Enable/disable player pin rendering                     |
| `.amir hide`           | Temporarily hide waypoint icons                         |
| `.amir show`           | Show waypoint icons again                               |

---

## Configuration

File: `altmapiconrenderer.json`

```json
{
  "square_waypoints": false,
  "player_colour": "#FFFFFF",
  "thin_outlines": false,
  "waypoint_scale": 1.0,
  "pin_player_icons": false
}
````

---

## Installation

Place the `.zip` or unpacked mod folder into your `Mods/` directory.

* No server-side dependency — works purely on the client.

---

## Compatibility

* Works with Vanilla Vintage Story
* Supports Vinconomy shop waypoints
* Does not affect other custom map layers unless patched (request for support)

---

## Known Limitations

* The color tint (e.g., red hover) only works cleanly if the icon texture is grayscale or flat-tinted.
* Some mods that use completely custom renderers may need manual patching to match behavior.