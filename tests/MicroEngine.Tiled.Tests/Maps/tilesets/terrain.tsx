<?xml version="1.0" encoding="UTF-8"?>
<tileset version="1.10" tiledversion="1.11.0" name="terrain" tilewidth="16" tileheight="16" tilecount="4" columns="2">
 <properties>
  <property name="biome" value="cave"/>
 </properties>
 <image source="terrain.png" width="32" height="32"/>
 <tile id="0">
  <properties>
   <property name="collision" value="solid"/>
  </properties>
 </tile>
 <tile id="1">
  <properties>
   <property name="collision" value="oneway"/>
  </properties>
 </tile>
 <tile id="2">
  <animation>
   <frame tileid="2" duration="100"/>
   <frame tileid="3" duration="300"/>
  </animation>
 </tile>
 <tile id="3" type="lava">
  <properties>
   <property name="collision" value="solid, hazard"/>
  </properties>
 </tile>
</tileset>
