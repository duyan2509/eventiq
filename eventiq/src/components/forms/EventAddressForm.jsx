import React from 'react';
import { Form, Select, Input, Button, Card} from 'antd';

const EventAddressForm = ({ 
    form, 
    loading, 
    provinces,
    communes,
    selectedProvince,
    onProvinceChange,
    onFinish 
}) => {
    return (
        <Card id="event-address" className="mb-8">
            <h2 className="text-xl font-semibold mb-4">Event Address</h2>
            <Form
                form={form}
                layout="vertical"
                onFinish={onFinish}
            >
                <Form.Item
                    name="provinceCode"
                    label="Province"
                    rules={[{ required: true }]}
                >
                    <Select
                        onChange={onProvinceChange}
                        options={provinces.map(p => ({ 
                            label: p.name, 
                            value: p.code 
                        }))}
                    />
                </Form.Item>

                <Form.Item
                    name="communeCode"
                    label="Commune"
                    rules={[{ required: true }]}
                >
                    <Select
                        disabled={!selectedProvince}
                        options={communes.map(c => ({ 
                            label: c.name, 
                            value: c.code 
                        }))}
                    />
                </Form.Item>

                <Form.Item
                    name="detail"
                    label="Address Detail"
                    rules={[{ required: true }]}
                >
                    <Input.TextArea />
                </Form.Item>

                <Form.Item>
                    <Button type="primary" htmlType="submit" loading={loading}>
                        Update Address
                    </Button>
                </Form.Item>
            </Form>
        </Card>
    );
};

export default EventAddressForm;