import bpy
import sys

# 명령줄에서 파라미터를 읽어오기
argv = sys.argv
argv = argv[argv.index("--") + 1:]  # -- 이후 인자를 파싱
bvh_in = argv[0]
fbx_out = argv[1]

# BVH 가져오기
bpy.ops.import_anim.bvh(filepath=bvh_in, filter_glob='*.bvh', global_scale=1, frame_start=1, use_fps_scale=False, use_cyclic=False, rotate_mode='NATIVE', axis_forward='-Z', axis_up='Y')

# 필요 없는 오브젝트 제거
for obj_name in ["Cube", "Lamp", "Camera"]:
    obj = bpy.data.objects.get(obj_name)
    if obj:
        bpy.data.objects.remove(obj, do_unlink=True)

# FBX로 내보내기
bpy.ops.export_scene.fbx(
        filepath=fbx_out,
        axis_forward='-Z',
        axis_up='Y',
        use_selection=True,
        bake_anim=True,  # 애니메이션 베이크
        apply_unit_scale=True  # 스케일 유지
    )
