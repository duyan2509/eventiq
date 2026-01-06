import React, { useState } from 'react';
import { Form, Input, Button, Card, Typography, App } from 'antd';
import { LockOutlined } from '@ant-design/icons';
import { useAuth } from '../contexts/AuthContext';
import { authAPI } from '../services/api';

const { Title } = Typography;

const UserProfile = () => {
    const { user } = useAuth();
    const { message } = App.useApp();
    const [passwordForm] = Form.useForm();
    const [passwordLoading, setPasswordLoading] = useState(false);

    const handleChangePassword = async (values) => {
        setPasswordLoading(true);
        try {
            await authAPI.changePassword({
                currentPassword: values.currentPassword,
                newPassword: values.newPassword,
            });
            message.success('Password changed successfully');
            passwordForm.resetFields();
        } catch (err) {
            message.error(err?.response?.data?.message || 'Failed to change password');
        } finally {
            setPasswordLoading(false);
        }
    };

    return (
        <div style={{ padding: '24px', maxWidth: '800px', margin: '0 auto' }}>
            <Title level={2}>Profile</Title>
            <Card>
                <Title level={3}>
                    <LockOutlined /> Change Password
                </Title>
                <Form
                    form={passwordForm}
                    layout="vertical"
                    onFinish={handleChangePassword}
                    autoComplete="off"
                >
                    <Form.Item
                        label="Current Password"
                        name="currentPassword"
                        rules={[{ required: true, message: 'Please enter current password' }]}
                    >
                        <Input.Password prefix={<LockOutlined />} />
                    </Form.Item>

                    <Form.Item
                        label="New Password"
                        name="newPassword"
                        rules={[
                            { required: true, message: 'Please enter new password' },
                            { min: 6, message: 'Password must be at least 6 characters' },
                        ]}
                    >
                        <Input.Password prefix={<LockOutlined />} />
                    </Form.Item>

                    <Form.Item
                        label="Confirm New Password"
                        name="confirmPassword"
                        dependencies={['newPassword']}
                        rules={[
                            { required: true, message: 'Please confirm new password' },
                            ({ getFieldValue }) => ({
                                validator(_, value) {
                                    if (!value || getFieldValue('newPassword') === value) {
                                        return Promise.resolve();
                                    }
                                    return Promise.reject(new Error('Passwords do not match'));
                                },
                            }),
                        ]}
                    >
                        <Input.Password prefix={<LockOutlined />} />
                    </Form.Item>

                    <Form.Item>
                        <Button 
                            type="primary" 
                            htmlType="submit" 
                            icon={<LockOutlined />}
                            loading={passwordLoading}
                        >
                            Change Password
                        </Button>
                    </Form.Item>
                </Form>
            </Card>
        </div>
    );
};

export default UserProfile;

