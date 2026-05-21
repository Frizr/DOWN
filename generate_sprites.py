import requests, base64, os, time, json
from pathlib import Path

A1111_URL = "http://localhost:7860"
OUTPUT_BASE = Path(r"D:\gamedev\down\Assets\Sprites\Generated")

def image_to_base64(path):
    with open(path, "rb") as f:
        return base64.b64encode(f.read()).decode("utf-8")

def generate(prompt, base_image_path, out_path):
    payload = {
        "init_images": [image_to_base64(base_image_path)],
        "denoising_strength": 0.5,
        "prompt": prompt,
        "negative_prompt": "blurry, realistic, 3D render, front view, multiple characters, background scenery, text, watermark, bad anatomy, extra limbs",
        "steps": 25,
        "cfg_scale": 7,
        "width": 512,
        "height": 512,
        "sampler_name": "DPM++ 2M Karras",
        "override_settings": {"sd_model_checkpoint": "pixelart.safetensors"}
    }
    r = requests.post(f"{A1111_URL}/sdapi/v1/img2img", json=payload)
    r.raise_for_status()
    img_data = r.json()["images"][0]
    out_path.parent.mkdir(parents=True, exist_ok=True)
    with open(out_path, "wb") as f:
        f.write(base64.b64decode(img_data))

PLAYER_BASE = r"D:\gamedev\down\Assets\Sprites\stay_player.jpg"
ENEMY_BASE  = r"D:\gamedev\down\Assets\Sprites\stay_enemy.jpg"

PLAYER_PREFIX = "isometric pixel art game sprite, green sci-fi space marine, heavy green armor, DOOM aesthetic, top-down 2.5D isometric view, pixel art style, transparent background, isolated character, "
ENEMY_PREFIX  = "isometric pixel art game sprite, black alien demon creature, red glowing single eye, dark scales, multiple claws, DOOM aesthetic, top-down 2.5D isometric view, pixel art style, transparent background, isolated character, "

ANIMATIONS = {
    "player": {
        "base": PLAYER_BASE,
        "prefix": PLAYER_PREFIX,
        "anims": {
            "idle":   ["standing still slight weight shift right","breathing chest expanded","weight centered","slight knee flex"],
            "walk":   ["right leg stepping forward","mid stride weight planted","left leg stepping forward","left foot planted","right heel lifting","stride completing"],
            "run":    ["sprinting lean forward right knee raised","right foot strike","both feet off ground airborne","left knee raised sprinting","left foot strike","second airborne"],
            "attack": ["right fist pulled back windup","right fist swinging forward","arm fully extended impact","arm retracting follow through","returning to guard stance"],
            "hurt":   ["body recoiling from hit","staggering off balance","crouching guard recovery"],
            "death":  ["massive impact body lurching","falling forward","knees hitting ground","body at 45 degrees","flat on ground motionless"],
        }
    },
    "enemy": {
        "base": ENEMY_BASE,
        "prefix": ENEMY_PREFIX,
        "anims": {
            "idle":   ["crouched menacing tendrils swaying","claws flexing eye glowing bright","body swaying left tail curling","swaying back to center"],
            "walk":   ["right claw stepping cautiously","weight shifting forward","left claw stepping","head scanning area","rear leg stepping","completing loop"],
            "chase":  ["lunging forward eye blazing","airborne claws reaching","front claws landing crouch","hind legs pushing off","full speed nearly horizontal","second airborne"],
            "attack": ["rearing back both claws raised high","claws swinging down with force","claws smashing ground impact","claws dragging follow through","pulling claws back recovery"],
            "hurt":   ["violent recoil from hit","staggering one limb buckling","rising with rage eye bright"],
            "death":  ["massive impact thrown sideways","front limbs collapsing","side of body hitting ground","sprawled minimal movement","completely flat and still"],
        }
    }
}

total = sum(len(v) for c in ANIMATIONS.values() for v in c["anims"].values())
done = 0

for char, data in ANIMATIONS.items():
    for anim_name, frames in data["anims"].items():
        for i, frame_desc in enumerate(frames):
            prompt = data["prefix"] + frame_desc
            out_path = OUTPUT_BASE / char / anim_name / f"frame_{i+1:02d}.png"
            if out_path.exists():
                print(f"[SKIP] {char}/{anim_name}/frame_{i+1:02d} already exists")
                done += 1
                continue
            done += 1
            print(f"[{done}/{total}] Generating {char} - {anim_name} - frame {i+1}/{len(frames)}")
            try:
                generate(prompt, data["base"], out_path)
                print(f"  Saved: {out_path}")
            except Exception as e:
                print(f"  ERROR: {e}")
            time.sleep(1)

print(f"\nDone! All frames saved to {OUTPUT_BASE}")
