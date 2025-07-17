# Vertx.EdgeKnife

Adds a "knife" to Shader Graph, VFX Graph, and Graph Toolkit[^1] windows.

[![Edge Knife](https://vertx.xyz/Images/EdgeKnife/edge-knife.gif)](https://vertx.xyz/Images/EdgeKnife/edge-knife.mp4)

> Like Fruit Ninja but without the fruit or ninja

## Usage
Hold <kbd>Ctrl</kbd><kbd>Right Mouse</kbd> to cut edges/wires.  
Hold <kbd>Shift</kbd><kbd>Right Mouse</kbd> to insert redirect/portal nodes.  

## Tested versions

| Unity       | Shader Graph/VFX Graph |
|-------------|------------------------|
| 2022.3.12f1 | 14.0.09                |
| 6000.0.8f1  | 17.0.03                |
| 6000.2.0b8  | 17.0.03                |

## Installation
[![openupm](https://img.shields.io/npm/v/com.vertx.edge-knife?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.vertx.edge-knife/)
  
<table><tr><td>
  
#### 1. Add the OpenUPM registry
1. Open `Edit/Project Settings/Package Manager`
1. Add a new Scoped Registry (or edit the existing OpenUPM entry):
   ```
   Name: OpenUPM
   URL:  https://package.openupm.com/
   Scope(s): com.vertx
   ```
1. **Save**

#### 2. Add the package
1. Open the Package Manager via `Window/Package Manager`.
1. Select the <kbd>+</kbd> from the top left of the window.
1. Select **Add package by Name** or **Add package from Git URL**.
1. Enter `com.vertx.edge-knife`.
1. Select **Add**.
  
</td></tr></table>
  
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/Z8Z42ZYHB)

[^1]: Graph Toolkit has its own freeform select and deletion bound to <kbd>Shift</kbd><kbd>Left Mouse</kbd> and <kbd>Alt</kbd><kbd>Shift</kbd><kbd>Left Mouse</kbd>.