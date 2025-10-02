import React, { useEffect, useState } from 'react';
import { Card, Button, Modal, Table, Form, Input, InputNumber, DatePicker, message, Typography, Popconfirm } from 'antd';
import dayjs from 'dayjs';
import { eventAPI } from '../../services/api';
import { useMessage } from '../../hooks/useMessage';
const TicketClassManager = ({ eventId }) => {
    const { error, success, contextHolder } = useMessage();
    const [ticketClasses, setTicketClasses] = useState([]);
    const [loading, setLoading] = useState(false);
    const [modalVisible, setModalVisible] = useState(false);
    const [editing, setEditing] = useState(false);
    const [current, setCurrent] = useState(null);
    const [form] = Form.useForm();
    const [initialValues, setInitialValues] = useState(null);
    const [isChanged, setIsChanged] = useState(false);

    const fetchTicketClasses = async () => {
        setLoading(true);
        try {
            const data = await eventAPI.getTicketClasses(eventId);
            setTicketClasses(Array.isArray(data) ? data : (data?.data || []));
        } catch (err) {
            error('Failed to fetch ticket classes');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        if (eventId) fetchTicketClasses();
    }, [eventId]);

    const openAddModal = () => {
        setEditing(false);
        setCurrent(null);
        setInitialValues({
            name: '',
            price: null,
            maxPerUser: null,
            saleStart: null,
            saleEnd: null,
        });
        form.resetFields();
        setModalVisible(true);
    };

    const openEditModal = (record) => {
        setEditing(true);
        setCurrent(record);
        const vals = {
            ...record,
            saleStart: record.saleStart ? dayjs(record.saleStart) : null,
            saleEnd: record.saleEnd ? dayjs(record.saleEnd) : null,
        };
        setInitialValues(vals);
        setModalVisible(true);
        setTimeout(() => {
            form.resetFields();
            form.setFieldsValue(vals);
        }, 0);
    };
    useEffect(() => {
        if (!modalVisible || !initialValues) {
            setIsChanged(false);
            return;
        }
        const handler = () => {
            const values = form.getFieldsValue();
            let changed = false;
            for (const key of Object.keys(initialValues)) {
                if (key === 'saleStart' || key === 'saleEnd') {
                    if ((initialValues[key] && values[key] && !dayjs(initialValues[key]).isSame(values[key])) || (!initialValues[key] && values[key]) || (initialValues[key] && !values[key])) {
                        changed = true;
                        break;
                    }
                } else {
                    if (initialValues[key] !== values[key]) {
                        changed = true;
                        break;
                    }
                }
            }
            setIsChanged(changed);
        };
        handler();
        const id = form.onFieldsChange?.(handler);
        return () => {
            if (id && form.unsubscribe) form.unsubscribe(id);
        };
    }, [modalVisible, initialValues, form]);


    const handleModalOk = async () => {
        try {
            const values = await form.validateFields();
            let payload = {};
            if (editing && current) {

                if (values.name !== initialValues.name) payload.name = values.name;
                if (values.price !== initialValues.price) payload.price = values.price;
                if (values.maxPerUser !== initialValues.maxPerUser) payload.maxPerUser = values.maxPerUser;
                if ((values.saleStart && !initialValues.saleStart) || (!values.saleStart && initialValues.saleStart) || (values.saleStart && initialValues.saleStart && !dayjs(values.saleStart).isSame(dayjs(initialValues.saleStart)))) {
                    payload.saleStart = values.saleStart ? values.saleStart.toISOString() : null;
                }
                if ((values.saleEnd && !initialValues.saleEnd) || (!values.saleEnd && initialValues.saleEnd) || (values.saleEnd && initialValues.saleEnd && !dayjs(values.saleEnd).isSame(dayjs(initialValues.saleEnd)))) {
                    payload.saleEnd = values.saleEnd ? values.saleEnd.toISOString() : null;
                }
                if (Object.keys(payload).length === 0) {
                    error('No changes detected');
                    return;
                }
                setLoading(true);
                await eventAPI.updateTicketClass(eventId, current.id, payload);
                success('Updated ticket class');
            } else {
                payload = {
                    ...values,
                    saleStart: values.saleStart ? values.saleStart.toISOString() : null,
                    saleEnd: values.saleEnd ? values.saleEnd.toISOString() : null,
                };
                setLoading(true);
                await eventAPI.addTicketClass(eventId, payload);
                success('Added ticket class');
            }
            setModalVisible(false);
            fetchTicketClasses();
        } catch (err) {
            error(err?.response?.data?.message || 'Error');
        } finally {
            setLoading(false);
        }
    };

    const handleDelete = async (record) => {
        setLoading(true);
        try {
            await eventAPI.deleteTicketClass(eventId, record.id);
            success('Deleted ticket class');
            fetchTicketClasses();
        } catch (err) {
            error(err?.response?.data?.message || 'Delete failed');
        } finally {
            setLoading(false);
        }
    };

    const columns = [
        { title: 'Name', dataIndex: 'name' },
        { title: 'Price', dataIndex: 'price' },
        { title: 'Max/User', dataIndex: 'maxPerUser' },
        { title: 'Sale Start', dataIndex: 'saleStart', render: v => v ? dayjs(v).format('YYYY-MM-DD HH:mm') : '-' },
        { title: 'Sale End', dataIndex: 'saleEnd', render: v => v ? dayjs(v).format('YYYY-MM-DD HH:mm') : '-' },
        {
            title: 'Action',
            render: (_, record) => (
                <>
                    <Button type="link" onClick={() => openEditModal(record)}>Edit</Button>
                    <Popconfirm
                        title="Delete Ticket Class"
                        description={`Are you sure you want to delete "${record.name}"?`}
                        okText="Delete"
                        okType="danger"
                        cancelText="Cancel"
                        onConfirm={() => handleDelete(record)}
                    >
                        <Button type="link" danger>Delete</Button>
                    </Popconfirm>
                </>
            ),
        },
    ];

    return (
        <Card className="mb-8">
            {contextHolder}
            <div className="flex justify-between mb-4">
                <Typography.Title level={4} className="!mb-0">Ticket Class Configuration</Typography.Title>
                <Button type="primary" onClick={openAddModal}>Add Ticket Class</Button>
            </div>
            <Table
                dataSource={ticketClasses}
                columns={columns}
                rowKey="id"
                loading={loading}
                pagination={false}
            />
            <Modal
                open={modalVisible}
                title={editing ? 'Update Ticket Class' : 'Add Ticket Class'}
                onCancel={() => {
                    setModalVisible(false);
                    setTimeout(() => form.resetFields(), 200);
                }}
                onOk={handleModalOk}
                okButtonProps={{ disabled: editing && !isChanged }}
                confirmLoading={loading}
                destroyOnHidden
            >
                <Form form={form} layout="vertical">
                    <Form.Item name="name" label="Name" rules={[{ required: true }]}>
                        <Input />
                    </Form.Item>
                    <Form.Item name="price" label="Price" rules={[{ required: true, type: 'number', min: 0, message: 'Price must be a number >= 0' }]}>
                        <InputNumber  style={{ width: '100%' }} type="number" min={0} />
                    </Form.Item>
                    <Form.Item name="maxPerUser" label="Max Per User" rules={[{ required: true, type: 'number', min: 1, message: 'Max per user must be >= 1' }]}>
                        <InputNumber style={{ width: '100%' }} type="number" min={1} />
                    </Form.Item>
                    <Form.Item name="saleStart" label="Sale Start">
                        <DatePicker showTime style={{ width: '100%' }} />
                    </Form.Item>
                    <Form.Item name="saleEnd" label="Sale End">
                        <DatePicker showTime style={{ width: '100%' }} />
                    </Form.Item>
                </Form>
            </Modal>
        </Card>
    );
};

export default TicketClassManager;
