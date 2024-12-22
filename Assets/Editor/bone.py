import os
import bpy
import sys


# 명령줄에서 파라미터를 읽어오기
argv = sys.argv
argv = argv[argv.index("--") + 1:]  # -- 이후 인자를 파싱

txt_in = argv[0]
obj_in=argv[1]
fbx_out = argv[2]


class Node(object):
    def __init__(self, name, pos):
        self.name = name
        self.pos = pos


class TreeNode(Node):
    def __init__(self, name, pos, parent=None):
        super(TreeNode, self).__init__(name, pos)
        self.children = []
        self.parent = parent


class Info:
    """
    Wrap class for rig information
    """
    def __init__(self, filename=None):
        self.joint_pos = {}
        self.joint_skin = []
        self.root = None
        if filename is not None:
            self.load(filename)

    def load(self, filename):
        with open(filename, 'r') as f_txt:
            lines = f_txt.readlines()
        for line in lines:
            word = line.split()
            if word[0] == 'joints':
                self.joint_pos[word[1]] = [float(word[2]), float(word[3]), float(word[4])]
            elif word[0] == 'root':
                root_pos = self.joint_pos[word[1]]
                self.root = TreeNode(word[1], (root_pos[0], root_pos[1], root_pos[2]))
            elif word[0] == 'skin':
                skin_item = word[1:]
                self.joint_skin.append(skin_item)
        self.loadHierarchy_recur(self.root, lines, self.joint_pos)

    def loadHierarchy_recur(self, node, lines, joint_pos):
        for li in lines:
            if li.split()[0] == 'hier' and li.split()[1] == node.name:
                pos = joint_pos[li.split()[2]]
                ch_node = TreeNode(li.split()[2], tuple(pos))
                node.children.append(ch_node)
                ch_node.parent = node
                self.loadHierarchy_recur(ch_node, lines, joint_pos)

    def save(self, filename):
        with open(filename, 'w') as file_info:
            for key, val in self.joint_pos.items():
                file_info.write(
                    'joints {0} {1:.8f} {2:.8f} {3:.8f}\n'.format(key, val[0], val[1], val[2]))
            file_info.write('root {}\n'.format(self.root.name))

            for skw in self.joint_skin:
                cur_line = 'skin {0} '.format(skw[0])
                for cur_j in range(1, len(skw), 2):
                    cur_line += '{0} {1:.4f} '.format(skw[cur_j], float(skw[cur_j+1]))
                cur_line += '\n'
                file_info.write(cur_line)

            this_level = self.root.children
            while this_level:
                next_level = []
                for p_node in this_level:
                    file_info.write('hier {0} {1}\n'.format(p_node.parent.name, p_node.name))
                    next_level += p_node.children
                this_level = next_level

    def save_as_skel_format(self, filename):
        fout = open(filename, 'w')
        this_level = [self.root]
        hier_level = 1
        while this_level:
            next_level = []
            for p_node in this_level:
                pos = p_node.pos
                parent = p_node.parent.name if p_node.parent is not None else 'None'
                line = '{0} {1} {2:8f} {3:8f} {4:8f} {5}\n'.format(hier_level, p_node.name, pos[0], pos[1], pos[2],
                                                                   parent)
                fout.write(line)
                for c_node in p_node.children:
                    next_level.append(c_node)
            this_level = next_level
            hier_level += 1
        fout.close()

    def normalize(self, scale, trans):
        for k, v in self.joint_pos.items():
            self.joint_pos[k] /= scale
            self.joint_pos[k] -= trans


        this_level = [self.root]
        while this_level:
            next_level = []
            for node in this_level:
                node.pos /= scale
                node.pos = (node.pos[0] - trans[0], node.pos[1] - trans[1], node.pos[2] - trans[2])
                for ch in node.children:
                    next_level.append(ch)
            this_level = next_level

    def get_joint_dict(self):
        joint_dict = {}
        this_level = [self.root]
        while this_level:
            next_level = []
            for node in this_level:
                joint_dict[node.name] = node.pos
                next_level += node.children
            this_level = next_level
        return joint_dict

    def adjacent_matrix(self):
        joint_pos = self.get_joint_dict()
        joint_name_list = list(joint_pos.keys())
        num_joint = len(joint_pos)
        adj_matrix = np.zeros((num_joint, num_joint))
        this_level = [self.root]
        while this_level:
            next_level = []
            for p_node in this_level:
                for c_node in p_node.children:
                    index_parent = joint_name_list.index(p_node.name)
                    index_children = joint_name_list.index(c_node.name)
                    adj_matrix[index_parent, index_children] = 1.
                next_level += p_node.children
            this_level = next_level
        adj_matrix = adj_matrix + adj_matrix.transpose()
        return adj_matrix

class ArmatureGenerator(object):
    def __init__(self, info, mesh=None):
        self._info = info
        self._mesh = mesh

    def generate(self, matrix=None):
        basename = self._mesh.name if self._mesh else ""
        arm_data = bpy.data.armatures.new(basename + "_armature")
        arm_obj = bpy.data.objects.new('brignet_rig', arm_data)

        bpy.context.collection.objects.link(arm_obj)
        bpy.context.view_layer.objects.active = arm_obj
        bpy.ops.object.mode_set(mode='EDIT')

        this_level = [self._info.root]
        hier_level = 1
        while this_level:
            next_level = []
            for p_node in this_level:
                pos = p_node.pos
                parent = p_node.parent.name if p_node.parent is not None else None

                e_bone = arm_data.edit_bones.new(p_node.name)
                if self._mesh and e_bone.name not in self._mesh.vertex_groups:
                    self._mesh.vertex_groups.new(name=e_bone.name)

                e_bone.head.x, e_bone.head.z, e_bone.head.y = pos[0], pos[2], pos[1]

                if parent:
                    e_bone.parent = arm_data.edit_bones[parent]
                    if e_bone.parent.tail == e_bone.head:
                        e_bone.use_connect = True

                if len(p_node.children) == 1:
                    pos = p_node.children[0].pos
                    e_bone.tail.x, e_bone.tail.z, e_bone.tail.y = pos[0], pos[2], pos[1]
                elif len(p_node.children) > 1:
                    x_offset = [abs(c_node.pos[0] - pos[0]) for c_node in p_node.children]

                    idx = x_offset.index(min(x_offset))
                    pos = p_node.children[idx].pos
                    e_bone.tail.x, e_bone.tail.z, e_bone.tail.y = pos[0], pos[2], pos[1]

                elif e_bone.parent:
                    offset = e_bone.head - e_bone.parent.head
                    e_bone.tail = e_bone.head + offset / 2
                else:
                    e_bone.tail.x, e_bone.tail.z, e_bone.tail.y = pos[0], pos[2], pos[1]
                    e_bone.tail.y += .1

                for c_node in p_node.children:
                    next_level.append(c_node)

            this_level = next_level
            hier_level += 1

        if matrix:
            arm_data.transform(matrix)

        bpy.ops.object.mode_set(mode='POSE')

        if self._mesh:
            for v_skin in self._info.joint_skin:
                v_idx = int(v_skin.pop(0))

                for i in range(0, len(v_skin), 2):
                    self._mesh.vertex_groups[v_skin[i]].add([v_idx], float(v_skin[i + 1]), 'REPLACE')

            arm_obj.matrix_world = self._mesh.matrix_world
            mod = self._mesh.modifiers.new('rignet', 'ARMATURE')
            mod.object = arm_obj

        return arm_obj
    
def load_rignet_skeleton():

    skel_path = txt_in
    obj_path = obj_in
    
    
    if not os.path.isfile(skel_path):
        print("Skeleton file not found!")
        return

    
    skel_info = Info(filename=skel_path)

  
    if os.path.isfile(obj_path):
        bpy.ops.wm.obj_import(filepath=obj_path, 
                      global_scale=1.0, 
                      forward_axis='NEGATIVE_Z', 
                      up_axis='Y', 
                      use_split_objects=True, 
                      import_vertex_groups=True)

        mesh_obj = bpy.context.selected_objects[0]

    
    ArmatureGenerator(skel_info, mesh_obj).generate()
    print("Armature generated successfully!")

load_rignet_skeleton()

mesh_obj = bpy.context.active_object



armature_obj = bpy.data.objects['brignet_rig']


bpy.context.view_layer.objects.active = mesh_obj
mesh_obj.select_set(True)
armature_obj.select_set(True)

bpy.ops.object.parent_set(type='ARMATURE_AUTO')

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
