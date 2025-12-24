import React, { useEffect, useState } from 'react';
import { Table, Button, Modal, Form, Input, Popconfirm, DatePicker, Select } from 'antd';
import { eventAPI } from '../services/api';
import dayjs from 'dayjs';
import { useMessage } from '../hooks/useMessage';

const { RangePicker } = DatePicker;

const EventItemManager = ({ eventId, orgId }) => {
    const { error, success, contextHolder } = useMessage();
    
    const [eventItems, setEventItems] = useState([]);
    const [charts, setCharts] = useState([]);
    const [isModalVisible, setIsModalVisible] = useState(false);
    const [isEditMode, setIsEditMode] = useState(false);
    const [selectedItem, setSelectedItem] = useState(null);
    const [form] = Form.useForm();
    const [loading, setLoading] = useState(false);

    const fetchEventItems = async () => {
        try {
            const response = await eventAPI.getEventItems(eventId);
            setEventItems(response);
        } catch (err) {
            error('Failed to fetch event items');
        }
    };

    const fetchCharts = async () => {
        try {
            const response = await eventAPI.getEventCharts(eventId);
            setCharts(response);
        } catch (err) {
            error('Failed to fetch charts');
        }
    };

    useEffect(() => {
        fetchEventItems();
        fetchCharts();
    }, [eventId]);

    const handleSubmit = async (values) => {
        setLoading(true);
        try {
            const data = {
                ...values,
                start: values.timeRange[0].toISOString(),
                end: values.timeRange[1].toISOString(),
            };
            delete data.timeRange;

            if (isEditMode) {
                await eventAPI.updateEventItem(eventId, selectedItem.id, data);
                success('Event item updated successfully');
            } else {
                await eventAPI.addEventItem(eventId, data);
                success('Event item added successfully');
            }
            setIsModalVisible(false);
            setIsEditMode(false);
            setSelectedItem(null);
            form.resetFields();
            fetchEventItems();
        } catch (err) {
            error(`Failed to ${isEditMode ? 'update' : 'add'} event item`);
        } finally {
            setLoading(false);
        }
    };

    const handleDelete = async (itemId) => {
        try {
            await eventAPI.deleteEventItem(eventId, itemId);
            success('Event item deleted successfully');
            fetchEventItems();
        } catch (err) {
            error('Failed to delete event item');
        }
    };

    const columns = [
        {
            title: 'Name',
            dataIndex: 'name',
            key: 'name',
        },
        {
            title: 'Description',
            dataIndex: 'description',
            key: 'description',
        },
        {
            title: 'Start Time',
            dataIndex: 'start',
            key: 'start',
            render: (text) => dayjs(text).format('YYYY-MM-DD HH:mm:ss'),
        },
        {
            title: 'End Time',
            dataIndex: 'end',
            key: 'end',
            render: (text) => dayjs(text).format('YYYY-MM-DD HH:mm:ss'),
        },
        {
            title: 'Chart',
            dataIndex: ['chart', 'name'],
            key: 'chartName',
        },
        {
            title: 'Action',
            key: 'action',
            render: (_, record) => (
                <div className="flex gap-2">
                    <Button
                        onClick={() => {
                            setIsEditMode(true);
                            setSelectedItem(record);
                            setIsModalVisible(true);
                            form.setFieldsValue({
                                name: record.name,
                                description: record.description,
                                chartId: record.chart.id,
                                timeRange: [dayjs(record.start), dayjs(record.end)],
                            });
                        }}
                    >
                        Edit
                    </Button>
                    <Popconfirm
                        title="Delete event item"
                        description="Are you sure to delete this event item?"
                        onConfirm={() => handleDelete(record.id)}
                        okText="Yes"
                        cancelText="No"
                    >
                        <Button danger>Delete</Button>
                    </Popconfirm>
                    <Button
                        type="primary"
                        onClick={() => {
                            window.open(`/org/${orgId}/event/${eventId}/seat-map/${record.chart.id}`, '_blank');
                        }}
                    >
                        Seat Map Setup
                    </Button>
                </div>
            ),
        },
    ];

    return (
        <div className="space-y-4">
            {contextHolder}
            <div className="flex justify-between items-center">
                <h2 className="text-xl font-semibold">Event Items</h2>
                <Button type="primary" onClick={() => setIsModalVisible(true)}>
                    Add Event Item
                </Button>
            </div>

            <Table
                dataSource={eventItems}
                columns={columns}
                rowKey="id"
            />

            <Modal
                title={isEditMode ? "Edit Event Item" : "Add New Event Item"}
                open={isModalVisible}
                onCancel={() => {
                    setIsModalVisible(false);
                    setIsEditMode(false);
                    setSelectedItem(null);
                    form.resetFields();
                }}
                footer={null}
            >
                <Form
                    form={form}
                    onFinish={handleSubmit}
                    layout="vertical"
                >
                    <Form.Item
                        name="name"
                        label="Event Item Name"
                        rules={[
                            { required: true, message: 'Please input event item name!' }
                        ]}
                    >
                        <Input />
                    </Form.Item>

                    <Form.Item
                        name="description"
                        label="Description"
                    >
                        <Input.TextArea />
                    </Form.Item>

                    <Form.Item
                        name="chartId"
                        label="Chart"
                        rules={[
                            { required: true, message: 'Please select a chart!' }
                        ]}
                    >
                        <Select>
                            {charts.map(chart => (
                                <Select.Option key={chart.id} value={chart.id}>
                                    {chart.name}
                                </Select.Option>
                            ))}
                        </Select>
                    </Form.Item>

                    <Form.Item
                        name="timeRange"
                        label="Time Range"
                        rules={[
                            { required: true, message: 'Please select time range!' }
                        ]}
                    >
                        <RangePicker 
                            showTime 
                            format="YYYY-MM-DD HH:mm:ss"
                        />
                    </Form.Item>

                    <div className="flex justify-end gap-2">
                        <Button onClick={() => {
                            setIsModalVisible(false);
                            setIsEditMode(false);
                            setSelectedItem(null);
                            form.resetFields();
                        }}>
                            Cancel
                        </Button>
                        <Button type="primary" htmlType="submit" loading={loading}>
                            {isEditMode ? 'Update' : 'Create'}
                        </Button>
                    </div>
                </Form>
            </Modal>
        </div>
    );
};

export default EventItemManager;