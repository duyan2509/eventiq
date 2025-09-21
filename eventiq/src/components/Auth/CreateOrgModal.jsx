import React, { useState } from 'react';
import { Modal, Form, Input, Button, Upload, Typography, Image } from 'antd';
import { PlusOutlined, LoadingOutlined } from '@ant-design/icons';
import { useMessage } from '../../hooks/useMessage';
import { organizationAPI } from '../../services/api';

const { Text } = Typography;

const CreateOrgModal = ({ visible, onCancel, onSuccess }) => {
    const [form] = Form.useForm();
    const [loading, setLoading] = useState(false);
    const [avatarFile, setAvatarFile] = useState(null);
    const [avatarPreview, setAvatarPreview] = useState(null);
    const { success, error, contextHolder } = useMessage();

    const handleChange = (info) => {
        const file = info.fileList.length > 0 ? info.fileList[info.fileList.length - 1].originFileObj : null;
        if (file) {
            setAvatarFile(file);
            setAvatarPreview(URL.createObjectURL(file));
        } else {
            setAvatarFile(null);
            setAvatarPreview(null);
        }
    };

    const handleRemove = () => {
        setAvatarFile(null);
        setAvatarPreview(null);
    };

    const handleSubmit = async (values) => {
        setLoading(true);
        try {
            console.log('Form Values:', values);
            console.log('Avatar File:', avatarFile);
            const res = await organizationAPI.create({ name: values.name, avatar: avatarFile });
            success('Create Organization Successfully!');
            form.resetFields();
            setAvatarFile(null);
            setAvatarPreview(null);
            onSuccess && onSuccess(res?.jwt);
        } catch (err) {
            error(err?.response?.data?.message || 'Create Organization Failed!');
        } finally {
            setLoading(false);
        }
    };

    const handleCancel = () => {
        form.resetFields();
        setAvatarFile(null);
        setAvatarPreview(null);
        onCancel();
    };

    return (
        <>
            {contextHolder}
            <Modal
                key={visible ? 'open' : 'closed'}
                title="Create new Organization"
                open={visible}
                onCancel={handleCancel}
                footer={null}
                width={400}
                centered
                className="rounded-lg"
            >
                <Form
                    form={form}
                    name="createOrg"
                    onFinish={handleSubmit}
                    layout="vertical"
                    requiredMark={false}
                >
                    <Form.Item
                        name="name"
                        label="Organization Name"
                        rules={[
                            { required: true, message: 'Please input organization name!' },
                            { min: 2, max: 30, message: 'Organization Name is 2-30' },
                        ]}
                    >
                        <Input placeholder="Input Organization name" size="large" className="rounded-lg" />
                    </Form.Item>

                    {/* Upload Avatar ngoài Form.Item */}
                    <div className="mb-4">
                        <label className="block mb-2 font-medium">Avatar</label>
                        <Upload
                            className="w-full flex justify-center items-center"
                            listType="picture-card"
                            showUploadList={false}
                            beforeUpload={() => false}
                            onChange={handleChange}
                            onRemove={handleRemove}
                            accept="image/*"
                            maxCount={1}
                        >
                            {!avatarPreview ? (
                                <div>
                                    <PlusOutlined />
                                    <div style={{ marginTop: 8 }}>Upload</div>
                                </div>
                            ) : (
                                <Image
                                    src={avatarPreview}
                                    alt="avatar preview"
                                    style={{ width: '100%', height: '100%', objectFit: 'cover', borderRadius: 8 }}
                                    preview={false}
                                />
                            )}
                        </Upload>
                    </div>

                    <Form.Item>
                        <Button
                            type="primary"
                            htmlType="submit"
                            loading={loading}
                            size="large"
                            block
                            className="h-12 rounded-lg bg-gradient-to-r from-blue-500 to-purple-600 border-none hover:from-blue-600 hover:to-purple-700"
                        >
                            Create new Organization
                        </Button>
                    </Form.Item>
                </Form>
            </Modal>
        </>
    );
};

export default CreateOrgModal;
