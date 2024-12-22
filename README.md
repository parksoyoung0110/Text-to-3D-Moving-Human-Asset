# Reference
Text/Image to 3D : https://github.com/Tencent/Hunyuan3D-1

Rig : https://github.com/zhan-xu/RigNet

Text to Human Motion : https://github.com/EricGuo5513/momask-codes


# Contribution
핵심 목표는 맞춤형 3D 캐릭터 모델을 생성하고 이를 자동화된 방식으로 애니메이션을 적용할 수 있는 파이프라인을 Unity에서 구축하는 것이다.

유니티의 Editor Tool을 통해 워크플로우를 자동화하는 코드를 작성하였다. 

이를 통해 3D 모델링, 리깅, 애니메이션 작업을 효율적으로 처리하고, 최종적으로 개발자가 손쉽게 자신만의 캐릭터 애니메이션을 통합할 수 있도록 하였다.  

각 단계에서 자동화된 프로세스를 통해, 개발자는 반복적인 작업을 줄이고 더 창의적인 부분에 집중할 수 있다.

1. BHV 파일을 FBX로 변환
   
   Momask로 만든 BHV 파일을 Blender에서 파이썬을 호출하여 FBX로 자동 변환한다. 

2. 3D Gen Model(Hunyuan3D)로 생성한 캐릭터 리깅
   
   3D Gen Model (Hunyuan3D)의 Image-to-3D를 이용해 맞춤형 3D 캐릭터 obj 파일과 RigNet의 rig.txt 파일 연결한다.
   
   파일을 기반으로 뼈대가 있는 아마처를 자동으로 생성하고 캐릭터 모델에 리깅을 적용한다.


# Code
Please check Assets>Editor for codes

1. Unity 에디터에서 경로 지정 후 Blender에서 파이썬 실행 (BVHImporter.cs & Bone.cs)

2. Blender에서 BVH  애니메이션 파일을 FBX 형식으로 변환 (bhv2fbx.py)

3. 스켈레톤 생성 후 캐릭터와 리깅 (bone.py)


# Assets
Assets>BHV

"A person is running on a treadmill.", "A person jumps up and then lands.", "The person does a salsa dance." 을 input으로한 momask의 결과 파일


Assets>animation_fbx

momask의 BHV파일을 BHVImporter를 통해 fbx로 변환한 파일


Assets>OBJ

Hunyuan3D-1로 생성한 캐릭터 5종


Assets>RIG

캐릭터 5종을 input으로 한 RigNet 결과 파일
