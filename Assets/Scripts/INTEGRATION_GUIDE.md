# Indoor Navigation AR - Integration Guide

## Complete Setup Instructions

### Phase 1: Project Configuration

#### 1.1 Scene Setup
```
Create New Scene: NavigationScene

GameObject Hierarchy Required:
??? AR Session (AR Foundation component)
??? AR Camera (AR Foundation component)
??? AR Anchor Manager (AR Foundation component)
??? AR Tracked Image Manager (AR Foundation component)
?   ??? Reference Image Library with QR codes/markers
??? NavigationSystemSetup (Empty GameObject)
?   ??? Script: NavigationSystemSetup.cs
??? Canvas (UI)
?   ??? Panel: MainPanel
?   ?   ??? Button: StartButton
?   ?   ??? Button: RecalibrateButton
?   ?   ??? Text: StatusText
?   ?   ??? Image: CalibrationIndicator
?   ??? Scroll View: DestinationList
?   ?   ??? Content
?   ?       ??? Prefab: DestinationListItem
?   ?       ??? Prefab: DestinationListItem
?   ??? Panel: NavigationPanel
?       ??? Text: DestinationName
?       ??? Text: Distance
?       ??? Text: Status
?       ??? Button: CancelButton
```

#### 1.2 Create DestinationListItem Prefab
```
Hierarchy:
DestinationListItem (Prefab)
??? Button
?   ??? Text: ItemName
?   ??? Text: ItemCategory
??? Script: DestinationListItem.cs
```

#### 1.3 JSON Graph File
- Location: `Assets/StreamingAssets/navigation_graph.json`
- Edit with NavigationGraphEditorWindow or manually
- Validate using "Validate Graph" button in editor

### Phase 2: Component Configuration

#### 2.1 NavigationController Setup
Inspector Settings:
```
Component References:
- QR Scanner: [Assign QRCodeScanner from scene]
- Localization Manager: [Assign LocalizationManager from scene]
- Path Renderer: [Assign PathRenderer from scene]
- UI Manager: [Assign NavigationUIManager from scene]
- Database: [Assign NavigationDatabase from scene]

Configuration:
- Navigation Graph File Name: "navigation_graph.json"
- Recalibration Check Interval: 1.0
- Waypoint Arrival Distance: 1.0
```

#### 2.2 QRCodeScanner Setup
Inspector Settings:
```
AR Setup:
- Camera Manager: [Assign ARCameraManager]
- Image Manager: [Assign ARTrackedImageManager]

Detection Settings:
- Scan Update Rate: 0.5
- Min Confidence Threshold: 0.7
- Max Scan Distance: 5.0
```

Important: Set up reference images in ARTrackedImageManager

#### 2.3 LocalizationManager Setup
Inspector Settings:
```
AR References:
- AR Session: [Assign ARSession]
- Anchor Manager: [Assign ARAnchorManager]

Settings:
- Recalibration Distance: 0.3
```

#### 2.4 PathRenderer Setup
Inspector Settings:
```
Rendering:
- Path Material: [Create material with Sprite shader]
- Arrow Material: [Create material with Standard shader]
- Path Width: 0.1
- Path Height: 0.01

Arrow Settings:
- Arrow Spacing: 1.0
- Arrow Size: 0.3
- Path Color: Cyan
- Next Segment Color: Yellow
```

#### 2.5 NavigationUIManager Setup
Inspector Settings:
```
UI Panels:
- Main Panel: [Assign Canvas/MainPanel]
- Destination List Panel: [Assign Canvas/DestinationList]
- Navigation Panel: [Assign Canvas/NavigationPanel]

References:
- Destination List Content: [Assign Scroll View Content]
- Destination Item Prefab: [Assign DestinationListItem prefab]
- Search Field: [Assign InputField component]
- Destination Name Text: [Assign navigation panel text]
- Distance Text: [Assign distance display text]
- Status Text: [Assign status display text]
- Calibration Indicator: [Assign indicator image]

Buttons:
- Start Navigation Button: [Assign start button]
- Cancel Navigation Button: [Assign cancel button]
- Recalibrate Button: [Assign recalibrate button]
```

#### 2.6 NavigationDatabase Setup
No configuration needed - automatic initialization

#### 2.7 NavigationSystemSetup Setup
Inspector Settings:
```
- Auto Initialize: true
- Show Debug Info: true
```

### Phase 3: QR Code Setup

#### 3.1 Create QR Codes
For each marker in your navigation graph:
```
Content: {MarkerId from JSON}
Example: "QR_ENTRANCE", "QR_CORRIDOR_A", "QR_OFFICE_101"

Tools:
- Online: qrcode.com, qr-code-generator.com
- Offline: ZXing library, python-qrcode
- Size: A4 or larger recommended
```

#### 3.2 Register Reference Images
In ARTrackedImageManager:
1. Create Reference Image Library asset
2. For each QR code/marker:
   - Import image or render to PNG
   - Set name = MarkerId (e.g., "QR_ENTRANCE")
   - Physical size: Actual dimensions in meters
3. Assign library to ARTrackedImageManager

#### 3.3 Place Markers
Physical placement:
- Position marker at exact node location
- Height: 1.0-1.5m (human eye level)
- Ensure good lighting, no glare
- Avoid reflective surfaces nearby

### Phase 4: Navigation Graph Creation

#### 4.1 Using Graph Editor (Recommended)
```
Window > Indoor Navigation > Graph Editor

Steps:
1. Click "New Graph"
2. Enter Building Name
3. Create nodes:
   - ID: unique identifier
   - Name: display name
   - Position: world coordinates in meters
   - Marker ID: matches QR code content
   - POI: check if destination
   - Category: Entrance, Office, Restroom, etc.
4. Click "Add Node"
5. Repeat for all nodes
6. Save Graph
7. Validate Graph
```

#### 4.2 Graph Design Best Practices
```
Node Placement:
- Place at corridor intersections
- Place at room entrances (POIs)
- Space: 5-15m between nodes typical
- Z-axis: use for perpendicular corridors

Connections:
- Connect adjacent nodes only
- Keep edge costs realistic (meters)
- Bidirectional: A?B and B?A

POI Types:
- Entrance: Main entry points
- Office: Rooms, conference rooms
- Facilities: Restrooms, elevators
- Emergency: Exits, emergency areas
```

#### 4.3 Testing Your Graph
```
Graph Validation Checklist:
? No duplicate node IDs
? All connections reference existing nodes
? Edge costs > 0
? At least one POI
? All nodes have realistic positions
? POIs have categories
? All markers have unique IDs

In Editor:
1. Load graph
2. Click "Validate Graph"
3. Check Console output
4. Fix any errors
5. Save and reload
```

### Phase 5: Testing & Debugging

#### 5.1 Initial Testing
```
Test Sequence:
1. Run scene in Unity Editor (with AR emulation)
   - Check Console for initialization messages
   - Verify all components loaded

2. Check Database
   - Verify JSON loaded correctly
   - Confirm node count matches

3. Test UI
   - Check destination list populated
   - Test search functionality
   - Verify button events

4. Test Pathfinding
   - Select destinations
   - Verify paths calculated
   - Check path validity in console
```

#### 5.2 Mobile Testing
```
Build & Deploy:
1. File > Build Settings
2. Select Android platform
3. Player Settings:
   - Package Name: com.indoornavigation.ar
   - Minimum API Level: 24
   - Target API Level: 33+
   - ARCore Required: Yes

4. Build and deploy to device

5. Test on actual device:
   - Run app
   - Initialize system
   - Select destination
   - Scan QR code
   - Verify path renders
   - Walk path and check accuracy
```

#### 5.3 Debug Output
```
Enable Console in App:
1. Add debug canvas to scene
2. Capture debug.unitylogger output
3. Display recent logs on screen

Expected Output:
[Navigation Setup] System initialization complete
[Navigation Database] Loaded graph 'Sample Building' with 8 nodes
[Navigation Controller] Initialization complete
[Navigation Controller] QR scanning started
[Navigation Controller] Destination selected: Office 101
[Navigation Controller] Path calculated with 3 waypoints
```

#### 5.4 Common Issues & Solutions

**Issue: Graph not loading**
```
Solution:
1. Check file at: Assets/StreamingAssets/navigation_graph.json
2. Verify JSON syntax (use jsonlint.com)
3. Check file permissions
4. Verify graph name matches parameter
5. Check Console for specific error
```

**Issue: QR codes not detected**
```
Solution:
1. Verify Reference Image Library created
2. Check MarkerId matches QR code content exactly
3. Test with good lighting
4. Increase m_ScanUpdateRate temporarily
5. Ensure ARSession is tracking
6. Check image quality (minimum 100x100 pixels)
```

**Issue: Path not rendering**
```
Solution:
1. Verify PathRenderer has materials assigned
2. Check nodes have valid positions
3. Verify graph loaded (check console)
4. Test pathfinding separately
5. Check UI layers not blocking AR
```

**Issue: Position inaccurate**
```
Solution:
1. Verify marker physical position matches JSON
2. Check calibration status (green indicator)
3. Scan marker again to recalibrate
4. Increase m_RecalibrationDistance if too sensitive
5. Ensure ARCore heavy tracking is active
```

### Phase 6: Performance Optimization

#### 6.1 Reduce CPU Usage
```
Adjustments:
- Increase m_ScanUpdateRate (0.5 ? 1.0)
- Reduce m_ArrowSpacing (more arrows = more CPU)
- Limit UI refresh rate
- Use smaller reference images
- Remove unnecessary scene objects
```

#### 6.2 Memory Optimization
```
Techniques:
- Clear path visualization when not navigating
- Cache frequently accessed nodes
- Unload unused scenes
- Monitor heap usage
```

#### 6.3 Battery Optimization
```
Settings:
- Reduce screen brightness
- Increase scan intervals
- Lower AR update rate
- Disable audio if possible
```

### Phase 7: Deployment Checklist

Pre-deployment:
```
? Graph validated
? All QR codes created and tested
? UI fully functional
? Pathfinding tested with multiple destinations
? Position accuracy within 30cm
? Recalibration < 1 second
? No console errors
? Performance acceptable (60 FPS)
? Battery life sufficient
? Works in target lighting conditions
? Documentation updated
? Code commented
? Build tested on target device
```

## Advanced Configuration

### Multi-Floor Support
```json
{
  "Nodes": [
    {
      "Id": "floor_1_entrance",
      "Metadata": {
        "Floor": "1",
        "Area": "Main"
      }
    },
    {
      "Id": "floor_2_entrance",
      "Metadata": {
        "Floor": "2",
        "Area": "Main"
      }
    }
  ]
}
```

### Custom Categories
```csharp
// Extend NavigationNode with custom properties
public struct Location
{
    public string Floor;
    public string Wing;
    public string AccessLevel; // Public, Staff, Restricted
}
```

### Voice Guidance Integration
```csharp
// Subscribe to waypoint events
pathRenderer.OnWaypointReached += (waypoint) => {
    textToSpeech.Speak($"Turn towards {waypoint.Name}");
};
```

## Troubleshooting Advanced Issues

### Graph Has Isolated Nodes
```
Check:
- All nodes are connected
- No dead-end nodes (unless intentional)
- Path exists between all POIs
- Use graph validation tool
```

### High Latency in Pathfinding
```
Optimize:
- Reduce node count
- Use more direct connections
- Cache common paths
- Implement path caching layer
```

### AR Jitter
```
Improve:
- Enable ARCore lightweight heavy motion mode
- Increase AR camera resolution
- Ensure good lighting
- Reduce path update frequency
```

## Support & Resources

- Unity AR Foundation Docs: https://docs.unity3d.com/Manual/xr-arfoundation-index.html
- ARCore Documentation: https://developers.google.com/ar
- A* Algorithm: https://en.wikipedia.org/wiki/A*_search_algorithm
- QR Code Format: https://www.qr-code-generator.com/about/

## Next Steps

1. Complete Phase 1-3 setup
2. Test in Unity Editor with emulation
3. Build and test on Android device
4. Create comprehensive navigation graph for your building
5. Deploy markers physically
6. Conduct user acceptance testing
7. Gather feedback and iterate

For questions or issues, check the console output and refer to the README.md in Scripts folder.
