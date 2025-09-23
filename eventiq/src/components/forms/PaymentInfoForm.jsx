import React from 'react';
import { Form, Select, Input, Button, Space, Card } from 'antd';

const PaymentInfoForm = ({ 
    form, 
    loading,
    lookupLoading,
    banks,
    onLookupClick,
    onFinish 
}) => {
    return (
        <Card id="payment-info" className="mb-8">
            <h2 className="text-xl font-semibold mb-4">Payment Information</h2>
            <Form
                form={form}
                layout="vertical"
                onFinish={onFinish}
            >
                <Form.Item
                    name="bankCode"
                    label="Bank"
                    rules={[{ required: true }]}
                >
                    <Select
                        options={banks.map(b => ({ 
                            label: b.short_name,
                            value: b.code,
                        }))}
                    />
                </Form.Item>

                <Form.Item
                    name="accountNumber"
                    label="Account Number"
                    rules={[{ required: true }]}
                >
                    <Input />
                </Form.Item>

                <Space className="mb-4">
                    <Button onClick={onLookupClick} loading={lookupLoading}>
                        Lookup Account
                    </Button>
                </Space>

                <Form.Item
                    name="accountName"
                    label="Account Name"
                    rules={[{ required: true }]}
                >
                    <Input readOnly />
                </Form.Item>

                <Form.Item>
                    <Button type="primary" htmlType="submit" loading={loading}>
                        Update Payment Information
                    </Button>
                </Form.Item>
            </Form>
        </Card>
    );
};

export default PaymentInfoForm;