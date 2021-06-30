import json
import numpy as np
import sys
from scipy import misc
import glob
import imageio

import os.path


type_dataset = str(sys.argv[1])

def get_absolute_depth_image(image,near = 0.8, far = 15):
    k1 = (far + near) / (far - near)
    k2 = (- 2.0 * far * near) / (far - near)

    divisor = np.max(image[:,:,0]) if np.max(image[:,:,0]) != 0 else 127.0 
    res = k2 / ((image[:,:,0] / divisor) - k1)  


    return res


sample_frames = {'rgb_in' : [], 'rgb_gt' : [], 'depth_gt' : [], 'fore_msk_gt' : [], 'fore_z_extr_gt' : [], 'back_msk_gt' : [], 'back_z_extr_gt' : []}

path = 'full_12k_samples_dataset_3_objects'
with open(f'{path}/logs/objects_relative_to_cam.json') as f:
  data = json.load(f)

frame_count = len(data['contentList'])

for current_i_frame in range(1,frame_count-1):


    if(type_dataset == 'test'):
        if(current_i_frame < 11001 or current_i_frame > 12001):
            continue
    if(type_dataset == 'val'):
        if(current_i_frame < 9001 or current_i_frame > 11001):
            continue
    if(type_dataset == 'train'):
        if(current_i_frame > 9000):
            break

    full_frame_index = data['contentList'][current_i_frame]['index']

    foreground_len = len(data['contentList'][current_i_frame]['foregroundObjects'])
    background_len = len(data['contentList'][current_i_frame]['backgroundObjects'])
    #Reading full RGB image
    rgb_image = imageio.imread(f'{path}/rgb/rgb_{full_frame_index + 1}.png')
    #Goal : rgb_in & rgb_gt : [B, 1, 64, 64, 3]
    #print(rgb_image[None,:,:,0:3].shape)

    sample_frames['rgb_in'].append(rgb_image[None,:,:,0:3])
    sample_frames['rgb_gt'].append(rgb_image[None,:,:,0:3])
    

    #Reading depth map
    depth_image = imageio.imread(f'{path}/depth_map/aov_image_' + format(full_frame_index-2, '04') + ".png")
    absolute_depth_image = get_absolute_depth_image(depth_image).astype('float32')
    
    #Goal : depth_gt [B, 1, 64, 64]

    sample_frames['depth_gt'].append(absolute_depth_image[None,:,:,None])
    
    foreground_mask_instances = []
    foreground_latent_instances = []
    for current_object in range(foreground_len):
        single_mask_instance = imageio.imread(f'{path}/instances/Instance_{full_frame_index+2+current_object}.png')
        restructured_single_mask_instance = (single_mask_instance[:,:,:] > 0).astype('bool')
        foreground_mask_instances.append(restructured_single_mask_instance)


        orientation = data['contentList'][current_i_frame]['foregroundObjects'][current_object]['orientation']
        scale = data['contentList'][current_i_frame]['foregroundObjects'][current_object]['scale']

        object_latent_variable = np.array(data['contentList'][current_i_frame]['foregroundObjects'][current_object]['position'] + [orientation, scale])
        foreground_latent_instances.append(object_latent_variable)
    
    #Goal 'fore_msk_gt' [B, N, 1, 64, 64]

    sample_frames['fore_msk_gt'].append(np.array(foreground_mask_instances)[:,None,:,:,0])
    
    #Goal 'fore_z_extr_gt' [B,N,1,5]

    sample_frames['fore_z_extr_gt'].append(np.array(foreground_latent_instances)[:,None,:])
    
    
    background_mask_instances = []
    background_latent_instances = []

    for current_object in range(background_len):
        single_mask_instance = imageio.imread(f'{path}/instances/Instance_{full_frame_index+2+foreground_len + current_object}.png')
        restructured_single_mask_instance = (single_mask_instance[:,:,:] > 0).astype('bool')
        background_mask_instances.append(restructured_single_mask_instance)

        orientation = data['contentList'][current_i_frame]['backgroundObjects'][current_object]['orientation']
        scale = data['contentList'][current_i_frame]['backgroundObjects'][current_object]['scale']

        object_latent_variable = np.array(data['contentList'][current_i_frame]['backgroundObjects'][current_object]['position'] + [orientation, scale])
        background_latent_instances.append(object_latent_variable)
    
    
    #Goal 'back_msk_gt' [B, N, 1, 64, 64]

    sample_frames['back_msk_gt'].append(np.array(background_mask_instances)[:,None,:,:,0])

    #Goal 'back_z_extr_gt' [B,N,1,5]

    sample_frames['back_z_extr_gt'].append(np.array(background_latent_instances)[:,None,:])
    
    if(current_i_frame % 100 == 0):
        print(f'{current_i_frame}/12000, {current_i_frame/12000}%')



#Goal : rgb_in & rgb_gt : [B, 1, 64, 64, 3]
print(np.array(sample_frames['rgb_in']).shape)
    #Goal : depth_gt [B, 1, 64, 64]
print(np.array(sample_frames['depth_gt']).shape)
    #Goal 'fore_msk_gt' [B, N, 1, 64, 64]
print(np.array(sample_frames['fore_msk_gt']).shape)
    #Goal 'fore_z_extr_gt' [B,N,1,5]
print(np.array(sample_frames['fore_z_extr_gt']).shape)
    #Goal 'back_msk_gt' [B, N, 1, 64, 64]
# print(np.array(sample_frames['back_msk_gt']).shape)
print(np.array(sample_frames['back_msk_gt']).shape)
    #Goal 'back_z_extr_gt' [B,N,1,5]
print(np.array(sample_frames['back_z_extr_gt']).shape)


mask = np.concatenate([sample_frames['fore_msk_gt'], sample_frames['back_msk_gt']], axis = 1)
obj_extrs = np.concatenate([sample_frames['fore_z_extr_gt'], sample_frames['back_z_extr_gt']], axis = 1)

sample_frames['mask'] = mask
sample_frames['obj_extrs'] = obj_extrs
## Saving them
split_ranges =  {'train' : (0,9000),'val' : (9000,10000), 'test' : (10000,12500)} 

for key,value in sample_frames.items():
    for split_type, split_range  in split_ranges.items():
        with open(f'{key}_{split_type}.npy', 'wb') as f:
            save_value = np.array(value[split_range[0]:split_range[1]])
            print(save_value.shape)
            np.save(key + '_' + split_type, save_value)
           
