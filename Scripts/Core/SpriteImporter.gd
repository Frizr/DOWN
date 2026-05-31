@tool
extends EditorScript

const PLAYER_DIR = "res://Assets/Sprites/Player/"
const ENEMY_DIR  = "res://Assets/Sprites/Enemy/Goblins/"
const NPC_DIR    = "res://Assets/Sprites/NPC/Soldiers/"
const OUT_DIR    = "res://Assets/SpriteFrames/"

const DIRS = ["down", "left", "right", "up"]

const PLAYER_ANIMS = {
	"Sword_Idle_full.png":   ["idle",   12, 6],
	"Sword_Walk_full.png":   ["walk",    6, 8],
	"Sword_Run_full.png":    ["run",     6, 12],
	"Sword_attack_full.png": ["attack",  8, 10],
	"Sword_Hurt_full.png":   ["hurt",    4, 10],
	"Sword_Death_full.png":  ["death",   7, 6],
}

const GOBLIN_ANIMS = {
	"walk":   ["walk",   6, 8],
	"attack": ["attack", 6, 10],
	"hurt":   ["hurt",   6, 10],
	"death":  ["death",  6, 6],
}

func _run():
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT_DIR))
	print("=== SpriteImporter: Starting ===")
	_create_player_frames()
	_create_goblin_frames()
	_create_npc_frames()
	print("=== Done! Files saved to: " + OUT_DIR + " ===")

func _create_player_frames():
	var sf = SpriteFrames.new()
	sf.remove_animation("default")
	for filename in PLAYER_ANIMS:
		var data = PLAYER_ANIMS[filename]
		var anim = data[0]; var fcols = data[1]; var fps = data[2]
		var path = PLAYER_DIR + filename
		if not ResourceLoader.exists(path): print("[SKIP] " + filename); continue
		var tex = load(path) as Texture2D
		for r in range(4):
			var dname = anim + "_" + DIRS[r]
			sf.add_animation(dname)
			sf.set_animation_speed(dname, fps)
			sf.set_animation_loop(dname, anim != "death" and anim != "hurt")
			for c in range(fcols):
				var a = AtlasTexture.new(); a.atlas = tex
				a.region = Rect2(c * 64, r * 64, 64, 64)
				sf.add_frame(dname, a)
		print("[Player] " + anim)
	ResourceSaver.save(sf, OUT_DIR + "player_frames.tres")
	print("[Player] Saved player_frames.tres")

func _create_goblin_frames():
	var units = {}
	_collect_goblin_pngs(ENEMY_DIR, units)
	if units.is_empty():
		print("[ERROR] No goblin sprites found")
		return
	for unit in units:
		var sf = SpriteFrames.new(); sf.remove_animation("default")
		for suffix in GOBLIN_ANIMS:
			if suffix not in units[unit]: continue
			var data = GOBLIN_ANIMS[suffix]
			var anim = data[0]; var fcols = data[1]; var fps = data[2]
			var tex = load(units[unit][suffix]) as Texture2D
			for r in range(4):
				var dname = anim + "_" + DIRS[r]
				sf.add_animation(dname); sf.set_animation_speed(dname, fps)
				sf.set_animation_loop(dname, anim != "death")
				for c in range(fcols):
					var a = AtlasTexture.new(); a.atlas = tex
					a.region = Rect2(c * 48, r * 48, 48, 48)
					sf.add_frame(dname, a)
		ResourceSaver.save(sf, OUT_DIR + unit.replace("-","_") + "_frames.tres")
		print("[Goblin] Saved " + unit)

func _collect_goblin_pngs(dir_path, units):
	var dir = DirAccess.open(dir_path)
	if dir == null:
		return
	dir.list_dir_begin()
	var fname = dir.get_next()
	while fname != "":
		var path = dir_path.path_join(fname)
		if dir.current_is_dir():
			_collect_goblin_pngs(path, units)
		elif fname.ends_with(".png"):
			_register_goblin_sprite(path, fname, units)
		fname = dir.get_next()
	dir.list_dir_end()

func _register_goblin_sprite(path, fname, units):
	var basename = fname.get_basename()
	for suffix in GOBLIN_ANIMS:
		var marker = "-" + suffix
		if basename.ends_with(marker):
			var unit = basename.substr(0, basename.length() - marker.length())
			var color = _goblin_color_from_path(path)
			var key = (color + "_" + unit).replace(" ", "_").replace("-", "_")
			if key not in units: units[key] = {}
			units[key][suffix] = path

func _goblin_color_from_path(path):
	if path.contains("green goblin version"):
		return "green"
	if path.contains("red goblin version"):
		return "red"
	return "goblin"

func _create_npc_frames():
	var dir = DirAccess.open(NPC_DIR)
	if dir == null: print("[ERROR] No NPC dir"); return
	dir.list_dir_begin()
	var fname = dir.get_next()
	while fname != "":
		if fname.ends_with(".png") and not fname.contains("Idle"):
			var tex = load(NPC_DIR + fname) as Texture2D
			var sf  = SpriteFrames.new(); sf.remove_animation("default")
			for r in range(4):
				var wn = "walk_" + DIRS[r]; sf.add_animation(wn)
				sf.set_animation_speed(wn, 6); sf.set_animation_loop(wn, true)
				for c in range(3):
					var a = AtlasTexture.new(); a.atlas = tex
					a.region = Rect2(c * 48, r * 48, 48, 48); sf.add_frame(wn, a)
				var idlen = "idle_" + DIRS[r]; sf.add_animation(idlen)
				sf.set_animation_speed(idlen, 4); sf.set_animation_loop(idlen, true)
				var ia = AtlasTexture.new(); ia.atlas = tex
				ia.region = Rect2(48, r * 48, 48, 48); sf.add_frame(idlen, ia)
			ResourceSaver.save(sf, OUT_DIR + fname.get_basename().to_lower() + "_frames.tres")
			print("[NPC] Saved " + fname)
		fname = dir.get_next()
