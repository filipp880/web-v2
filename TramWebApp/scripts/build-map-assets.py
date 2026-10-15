"""Build offline Moscow map and demo tram geometry from an OSM API map extract.
Source: https://api.openstreetmap.org/api/0.6/map?bbox=37.635,55.757,37.65,55.77
OSM contributors, ODbL 1.0. Run from TramWebApp with the extract in tram-area.osm.
"""
import heapq
import json
import math
from pathlib import Path
import xml.etree.ElementTree as ET

root = ET.parse('tram-area.osm').getroot()
nodes = {n.attrib['id']: [float(n.attrib['lat']), float(n.attrib['lon'])] for n in root.findall('node')}
graph = {}
features = []
def length(a, b):
    return math.hypot((a[0] - b[0]) * 111195, (a[1] - b[1]) * 111195 * math.cos(math.radians(a[0])))
for way in root.findall('way'):
    tags = {t.attrib['k']: t.attrib['v'] for t in way.findall('tag')}
    refs = [n.attrib['ref'] for n in way.findall('nd') if n.attrib['ref'] in nodes]
    if len(refs) < 2:
        continue
    if tags.get('railway') == 'tram':
        for a, b in zip(refs, refs[1:]):
            graph.setdefault(a, []).append((b, length(nodes[a], nodes[b])))
            graph.setdefault(b, []).append((a, length(nodes[a], nodes[b])))
    kind = 'tram' if tags.get('railway') == 'tram' else 'road' if 'highway' in tags and tags['highway'] not in ('steps', 'footway', 'path', 'service') else 'building' if 'building' in tags else 'water' if tags.get('natural') == 'water' else 'park' if tags.get('leisure') in ('park', 'garden') else None
    if kind:
        coords = [[nodes[n][1], nodes[n][0]] for n in refs]
        polygon = kind in ('building', 'water', 'park') and refs[0] == refs[-1]
        features.append({'type': 'Feature', 'properties': {'kind': kind, 'name': tags.get('name:ru', tags.get('name', ''))}, 'geometry': {'type': 'Polygon' if polygon else 'LineString', 'coordinates': [coords] if polygon else coords}})
start = min(graph, key=lambda n: length(nodes[n], [55.7656, 37.6385]))
queue = [(0, start)]
distances, parents = {start: 0}, {}
while queue:
    distance, current = heapq.heappop(queue)
    if distance != distances[current]:
        continue
    for neighbor, weight in graph[current]:
        candidate = distance + weight
        if candidate < distances.get(neighbor, float('inf')):
            distances[neighbor], parents[neighbor] = candidate, current
            heapq.heappush(queue, (candidate, neighbor))
end = min(distances, key=lambda n: length(nodes[n], [55.7571, 37.6468]))
path = [end]
while path[-1] != start:
    path.append(parents[path[-1]])
route = [nodes[n] for n in reversed(path)]
destination = Path('src/TramWebApp.Infrastructure/Persistence/Data')
destination.mkdir(parents=True, exist_ok=True)
(destination / 'moscow-tram.json').write_text(json.dumps(route, separators=(',', ':')), encoding='utf-8')
public = Path('frontend/public/maps')
public.mkdir(parents=True, exist_ok=True)
(public / 'moscow.geojson').write_text(json.dumps({'type': 'FeatureCollection', 'features': features}, ensure_ascii=False, separators=(',', ':')), encoding='utf-8')
print(f'Route: {len(route)} vertices, {distances[end]:.0f} m; offline map: {len(features)} features')
