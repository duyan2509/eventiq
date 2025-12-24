import React, { useState, useEffect } from 'react';
import { Form, Input, Button, Card, Typography, message, Space } from 'antd';
import { UserOutlined, SaveOutlined } from '@ant-design/icons';
import { useAuth } from '../contexts/AuthContext';
import { authAPI } from '../services/api';

const { Title } = Typography;

const AdminProfile = () => {
    const { user } = useAuth();
    const [form] = Form.useForm();
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        if (user) {
            form.setFieldsValue({
                firstName: user.firstName || '',
                lastName: user.lastName || '',
                email: user.email || '',
                phoneNumber: user.phoneNumber || '',
            });
        }
    }, [user, form]);

    const handleSubmit = async (values) => {
        setLoading(true);
        try {
            // TODO: Implement update profile API
            // await authAPI.updateProfile(values);
            message.success('Cập nhật profile thành công');
        } catch (err) {
            message.error(err?.response?.data?.message || 'Cập nhật profile thất bại');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div>
            <Title level={2}>
                <UserOutlined /> Profile
            </Title>
            <Card>
                <Form
                    form={form}
                    layout="vertical"
                    onFinish={handleSubmit}
                    autoComplete="off"
                >
                    <Form.Item
                        label="Họ"
                        name="firstName"
                        rules={[{ required: true, message: 'Vui lòng nhập họ' }]}
                    >
                        <Input prefix={<UserOutlined />} />
                    </Form.Item>

                    <Form.Item
                        label="Tên"
                        name="lastName"
                        rules={[{ required: true, message: 'Vui lòng nhập tên' }]}
                    >
                        <Input prefix={<UserOutlined />} />
                    </Form.Item>

                    <Form.Item
                        label="Email"
                        name="email"
                        rules={[
                            { required: true, message: 'Vui lòng nhập email' },
                            { type: 'email', message: 'Email không hợp lệ' },
                        ]}
                    >
                        <Input disabled />
                    </Form.Item>

                    <Form.Item
                        label="Số điện thoại"
                        name="phoneNumber"
                    >
                        <Input />
                    </Form.Item>

                    <Form.Item>
                        <Space>
                            <Button 
                                type="primary" 
                                htmlType="submit" 
                                icon={<SaveOutlined />}
                                loading={loading}
                            >
                                Lưu thay đổi
                            </Button>
                            <Button onClick={() => form.resetFields()}>
                                Đặt lại
                            </Button>
                        </Space>
                    </Form.Item>
                </Form>
            </Card>
        </div>
    );
};

export default AdminProfile;

