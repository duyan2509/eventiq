import React, { useState } from 'react';
import { Form, Input, Button, Modal } from 'antd';

const PasswordVerificationForm = ({ visible, onCancel, onConfirm, loading }) => {
  const [form] = Form.useForm();

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      onConfirm(values.password);
    } catch (error) {
      console.error('Validation failed:', error);
    }
  };

  return (
    <Modal
      title="Verify Password"
      open={visible}
      onCancel={onCancel}
      footer={[
        <Button key="cancel" onClick={onCancel}>
          Cancel
        </Button>,
        <Button key="submit" type="primary" loading={loading} onClick={handleSubmit}>
          Verify
        </Button>,
      ]}
    >
      <Form form={form} layout="vertical">
        <Form.Item
          name="password"
          label="Password"
          rules={[{ required: true, message: 'Please enter your password' }]}
        >
          <Input.Password placeholder="Enter your password" />
        </Form.Item>
      </Form>
    </Modal>
  );
};

export default PasswordVerificationForm;
